using Lumen.BusinessLogic.Services;
using Lumen.DataAccess.Repositories;
using Lumen.Shared.Enums;
using Lumen.Shared.Exceptions;
using Moq;

namespace Lumen.BusinessLogic.Tests;

public sealed class ReportServiceTests
{
    [Theory]
    [InlineData(2026, 2, 1, 2026, 1, 1)]
    [InlineData(2026, 10, 6, 2026, 10, 5)]
    public async Task GetSummaryAsync_RejectsInvertedRange(
        int fromYear, int fromMonth, int fromDay, int toYear, int toMonth, int toDay)
    {
        var service = new ReportService(
            Mock.Of<IReportRepository>(),
            new TestCurrentUser { Role = UserRole.Administrador });

        await Assert.ThrowsAsync<InvalidDateRangeException>(() =>
            service.GetSummaryAsync(new DateTime(fromYear, fromMonth, fromDay), new DateTime(toYear, toMonth, toDay)));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public async Task GetSummaryAsync_ReturnsRepositoryCounts(int fromDay)
    {
        var from = new DateTime(2026, 10, fromDay);
        var to = new DateTime(2026, 10, 5);
        var toExclusiveUtc = new DateTime(2026, 10, 6, 0, 0, 0, DateTimeKind.Utc);
        using var cancellation = new CancellationTokenSource();
        var repository = new Mock<IReportRepository>(MockBehavior.Strict);
        repository.Setup(x => x.GetCountsAsync(
                It.Is<DateTime>(value => value == from && value.Kind == DateTimeKind.Utc),
                It.Is<DateTime?>(value => value == toExclusiveUtc && value.Value.Kind == DateTimeKind.Utc),
                cancellation.Token))
            .ReturnsAsync(new ReportCounts(4, 2, 3));
        var service = new ReportService(repository.Object, new TestCurrentUser { Role = UserRole.Administrador });

        var result = await service.GetSummaryAsync(from, to, cancellation.Token);

        Assert.Equal(4, result.RegisteredUsers);
        Assert.Equal(2, result.CreatedCourses);
        Assert.Equal(3, result.RequestedEnrollments);
        Assert.Equal(from, result.From);
        Assert.Equal(to, result.To);
        Assert.Equal(DateTimeKind.Utc, result.From.Kind);
        Assert.Equal(DateTimeKind.Utc, result.To.Kind);
        repository.VerifyAll();
    }

    [Fact]
    public async Task GetSummaryAsync_WithLastRepresentableDay_DoesNotOverflow()
    {
        var from = new DateTime(9999, 12, 31, 0, 0, 0, DateTimeKind.Utc);
        var repository = new Mock<IReportRepository>(MockBehavior.Strict);
        repository.Setup(x => x.GetCountsAsync(from, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReportCounts(1, 2, 3));
        var service = new ReportService(repository.Object, new TestCurrentUser { Role = UserRole.Administrador });

        var result = await service.GetSummaryAsync(from, from);

        Assert.Equal(from, result.To);
        Assert.Equal(1, result.RegisteredUsers);
        repository.VerifyAll();
    }

    [Theory]
    [InlineData(UserRole.Alumno)]
    [InlineData(UserRole.Profesor)]
    public async Task GetSummaryAsync_WithoutAdministratorRole_ThrowsForbidden(UserRole role)
    {
        var repository = new Mock<IReportRepository>(MockBehavior.Strict);
        var service = new ReportService(repository.Object, new TestCurrentUser { Role = role });

        await Assert.ThrowsAsync<ForbiddenOperationException>(() =>
            service.GetSummaryAsync(new DateTime(2026, 10, 1), new DateTime(2026, 10, 5)));
        repository.VerifyNoOtherCalls();
    }
}
