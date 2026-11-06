using Lumen.DataAccess;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Lumen.Migrations.DatabaseMigrations;

[DbContext(typeof(LumenDbContext))]
[Migration("20261106230000_EnforceCourseCapacity")]
public sealed class EnforceCourseCapacity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>("ResolutionReason", "Enrollments", "TEXT", maxLength: 200, nullable: true);
        migrationBuilder.Sql("""
            CREATE TRIGGER TR_CourseRevisions_Capacity
            BEFORE UPDATE OF MaxCapacity ON CourseRevisions
            WHEN NEW.MaxCapacity < OLD.MaxCapacity AND NEW.MaxCapacity < (
                SELECT COUNT(*) FROM Enrollments AS e
                WHERE e.CourseId = NEW.CourseId AND e.Status = 'Aprobada')
            BEGIN
                SELECT RAISE(ABORT, 'El cupo no puede quedar por debajo de las inscripciones aprobadas.');
            END;
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER TR_Courses_PublishedCapacity
            BEFORE UPDATE OF PublishedRevisionId ON Courses
            WHEN NEW.PublishedRevisionId IS NOT NULL AND
                (SELECT MaxCapacity FROM CourseRevisions WHERE Id = NEW.PublishedRevisionId) <
                (SELECT COUNT(*) FROM Enrollments AS e
                 WHERE e.CourseId = NEW.Id AND e.Status = 'Aprobada')
            BEGIN
                SELECT RAISE(ABORT, 'El cupo publicado no alcanza para las inscripciones aprobadas.');
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TRIGGER TR_Courses_PublishedCapacity;");
        migrationBuilder.Sql("DROP TRIGGER TR_CourseRevisions_Capacity;");
        migrationBuilder.Sql("ALTER TABLE Enrollments DROP COLUMN ResolutionReason;");
    }
}
