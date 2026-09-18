using Lumen.DataAccess.Entities;
using Lumen.DataAccess.Repositories;
using Lumen.Shared.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Lumen.DataAccess;

public sealed class DbInitializer(
    LumenDbContext context,
    IUserRepository userRepository,
    ICourseRepository courseRepository,
    IEnrollmentRepository enrollmentRepository,
    IPasswordHasher<User> passwordHasher)
{
    public const string DemoPassword = "Lumen123!";

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await context.Database.MigrateAsync(cancellationToken);
        if (await context.Users.AsNoTracking().AnyAsync(cancellationToken))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var student = await CreateUserAsync("alumno@lumen.local", UserRole.Alumno, "María", "Pérez", "30111222", now, cancellationToken);
        var teacher = await CreateUserAsync("profesor@lumen.local", UserRole.Profesor, "Daniel", "Torres", "20222333", now, cancellationToken);
        var admin = await CreateUserAsync("admin@lumen.local", UserRole.Administrador, "Lucía", "Admin", "10111444", now, cancellationToken);

        var reactCourse = await CreatePublishedCourseAsync(
            teacher,
            "Desarrollo Web con React",
            "Construcción de interfaces web modernas mediante componentes.",
            "Desarrollo Web",
            [
                ("Fundamentos de componentes", "Componentes, propiedades y estado.", 90, "Introducción práctica a componentes.", new[] { "guia-componentes.pdf" }),
                ("Estado y efectos", "Manejo de estado y efectos.", 120, "Ejercicios de estado y efectos.", new[] { "ejercicios-react.zip" }),
                ("Proyecto integrador", "Aplicación completa.", 180, "Consigna del proyecto final.", new[] { "consigna-proyecto.pdf" })
            ],
            now,
            cancellationToken);

        var uxCourse = await CreatePublishedCourseAsync(
            teacher,
            "UX/UI Design",
            "Diseño de experiencias e interfaces centradas en las personas.",
            "Diseño",
            [
                ("Investigación", "Fundamentos de investigación de usuarios.", 75, "Guía de entrevistas.", new[] { "entrevistas.pdf" }),
                ("Prototipado", "Wireframes y prototipos navegables.", 105, "Actividad de prototipado.", new[] { "plantilla-wireframe.fig" })
            ],
            now.AddDays(-2),
            cancellationToken);

        var approved = await enrollmentRepository.CreateAsync(new Enrollment
        {
            StudentId = student.Id,
            CourseId = reactCourse.Id,
            Status = EnrollmentStatus.Aprobada,
            RequestedAtUtc = now.AddDays(-10),
            ResolvedAtUtc = now.AddDays(-9),
            ResolvedByUserId = admin.Id
        }, cancellationToken);

        var firstModuleId = reactCourse.PublishedRevision!.Modules.OrderBy(x => x.Order).First().Id;
        approved.ModuleCompletions.Add(new EnrollmentModuleCompletion
        {
            EnrollmentId = approved.Id,
            ModuleId = firstModuleId,
            CompletedAtUtc = now.AddDays(-4)
        });
        await enrollmentRepository.SaveAsync(cancellationToken);

        await enrollmentRepository.CreateAsync(new Enrollment
        {
            StudentId = student.Id,
            CourseId = uxCourse.Id,
            Status = EnrollmentStatus.Pendiente,
            RequestedAtUtc = now.AddDays(-1)
        }, cancellationToken);
    }

    private async Task<User> CreateUserAsync(
        string email,
        UserRole role,
        string firstName,
        string lastName,
        string dni,
        DateTime createdAt,
        CancellationToken cancellationToken)
    {
        var user = new User
        {
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            Role = role,
            FirstName = firstName,
            LastName = lastName,
            Dni = dni,
            Phone = "11-5555-0000",
            Address = "Av. Lumen 123",
            PostalCode = "1000",
            IsActive = true,
            CreatedAtUtc = createdAt
        };
        user.PasswordHash = passwordHasher.HashPassword(user, DemoPassword);
        return await userRepository.CreateAsync(user, cancellationToken);
    }

    private async Task<Course> CreatePublishedCourseAsync(
        User teacher,
        string title,
        string description,
        string category,
        IReadOnlyList<(string Name, string Description, int Duration, string Content, string[] Resources)> moduleData,
        DateTime createdAt,
        CancellationToken cancellationToken)
    {
        var course = new Course
        {
            CreatedByUserId = teacher.Id,
            OwnerProfessorId = teacher.Id,
            OperationalStatus = CourseOperationalStatus.Activo,
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = createdAt
        };
        var revision = new CourseRevision
        {
            Version = 1,
            Status = CourseRevisionStatus.Borrador,
            Title = title,
            Description = description,
            Category = category,
            Level = "Inicial",
            Modality = "Online",
            MaxCapacity = 30,
            LearningObjectives = ["Aplicar los conceptos principales del curso."],
            SuggestedPrerequisites = ["Conocimientos básicos de informática."],
            CreatedAtUtc = createdAt,
            UpdatedAtUtc = createdAt
        };
        var order = 1;
        foreach (var item in moduleData)
        {
            revision.Modules.Add(new CourseModule
            {
                Order = order++,
                Name = item.Name,
                Description = item.Description,
                DurationMinutes = item.Duration,
                Content = item.Content,
                Resources = [.. item.Resources]
            });
        }
        revision.TotalDurationMinutes = revision.Modules.Sum(x => x.DurationMinutes);
        await courseRepository.CreateAsync(course, revision, cancellationToken);
        revision.Status = CourseRevisionStatus.Publicado;
        revision.PublishedAtUtc = createdAt;
        course.PublishedRevisionId = revision.Id;
        await courseRepository.SaveAsync(cancellationToken);
        course.PublishedRevision = revision;
        return course;
    }
}

