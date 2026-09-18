using Lumen.BusinessLogic.Abstractions;
using Lumen.BusinessLogic.Services;
using Lumen.DataAccess.Entities;
using Lumen.DataAccess.Repositories;
using Lumen.Shared.DTOs;
using Lumen.Shared.Enums;
using Lumen.Shared.Exceptions;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace Lumen.BusinessLogic.Tests;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_CreatesStudentAndHashesPassword()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(x => x.EmailExistsAsync("ALUMNO@TEST.COM", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        users.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User user, CancellationToken _) => { user.Id = Guid.NewGuid(); return user; });
        var hasher = new Mock<IPasswordHasher<User>>();
        hasher.Setup(x => x.HashPassword(It.IsAny<User>(), "secreto")).Returns("hash");
        var token = new Mock<ITokenService>();
        var service = new AuthService(users.Object, hasher.Object, token.Object);

        var result = await service.RegisterAsync(new UserCreateDTO(
            "alumno@test.com", "secreto", UserRole.Alumno, "Ana", "Pérez", "123", "555", "Calle 1", "1000"));

        Assert.Equal(UserRole.Alumno, result.Role);
        users.Verify(x => x.CreateAsync(It.Is<User>(u => u.PasswordHash == "hash"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_RejectsAdministratorRole()
    {
        var service = new AuthService(
            Mock.Of<IUserRepository>(),
            Mock.Of<IPasswordHasher<User>>(),
            Mock.Of<ITokenService>());

        await Assert.ThrowsAsync<PublicRegistrationRoleNotAllowedException>(() =>
            service.RegisterAsync(new UserCreateDTO(
                "admin@test.com", "secret", UserRole.Administrador, "A", "B", "1", "2", "C", "3")));
    }

    [Fact]
    public async Task RegisterAsync_RejectsDuplicateEmail()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(x => x.EmailExistsAsync("A@B.COM", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var service = new AuthService(users.Object, Mock.Of<IPasswordHasher<User>>(), Mock.Of<ITokenService>());

        await Assert.ThrowsAsync<DuplicateEmailException>(() =>
            service.RegisterAsync(new UserCreateDTO("a@b.com", "x", UserRole.Alumno, "A", "B", "1", "2", "C", "3")));
    }

    [Fact]
    public async Task LoginAsync_RejectsInvalidPassword()
    {
        var user = new User { Email = "a@b.com", NormalizedEmail = "A@B.COM", IsActive = true, PasswordHash = "hash" };
        var users = new Mock<IUserRepository>();
        users.Setup(x => x.GetByEmailAsync("A@B.COM", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var hasher = new Mock<IPasswordHasher<User>>();
        hasher.Setup(x => x.VerifyHashedPassword(user, "hash", "bad")).Returns(PasswordVerificationResult.Failed);
        var service = new AuthService(users.Object, hasher.Object, Mock.Of<ITokenService>());

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            service.LoginAsync(new LoginRequestDTO("a@b.com", "bad")));
    }
}

