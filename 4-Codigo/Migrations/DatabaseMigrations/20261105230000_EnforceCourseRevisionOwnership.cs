using Lumen.DataAccess;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Lumen.Migrations.DatabaseMigrations;

[DbContext(typeof(LumenDbContext))]
[Migration("20261105230000_EnforceCourseRevisionOwnership")]
public sealed class EnforceCourseRevisionOwnership : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        AddPointerGuard(migrationBuilder, "Insert", "INSERT");
        AddPointerGuard(migrationBuilder, "Update", "UPDATE OF WorkingRevisionId, PublishedRevisionId");
        migrationBuilder.Sql("""
            CREATE TRIGGER TR_CourseRevisions_KeepReferencedOwner
            BEFORE UPDATE OF CourseId ON CourseRevisions
            WHEN NEW.CourseId <> OLD.CourseId AND EXISTS (
                SELECT 1 FROM Courses AS c
                WHERE c.Id <> NEW.CourseId
                    AND (c.WorkingRevisionId = OLD.Id OR c.PublishedRevisionId = OLD.Id)
            )
            BEGIN
                SELECT RAISE(ABORT, 'Una revisión referenciada debe conservar su curso.');
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP TRIGGER TR_Courses_RevisionOwner_Insert;");
        migrationBuilder.Sql("DROP TRIGGER TR_Courses_RevisionOwner_Update;");
        migrationBuilder.Sql("DROP TRIGGER TR_CourseRevisions_KeepReferencedOwner;");
    }

    private static void AddPointerGuard(MigrationBuilder migrationBuilder, string suffix, string operation)
    {
        migrationBuilder.Sql($"""
            CREATE TRIGGER TR_Courses_RevisionOwner_{suffix}
            BEFORE {operation} ON Courses
            WHEN (NEW.WorkingRevisionId IS NOT NULL AND NOT EXISTS (
                SELECT 1 FROM CourseRevisions WHERE Id = NEW.WorkingRevisionId AND CourseId = NEW.Id
            )) OR (NEW.PublishedRevisionId IS NOT NULL AND NOT EXISTS (
                SELECT 1 FROM CourseRevisions WHERE Id = NEW.PublishedRevisionId AND CourseId = NEW.Id
            ))
            BEGIN
                SELECT RAISE(ABORT, 'Las revisiones de trabajo y publicada deben pertenecer al curso.');
            END;
            """);
    }
}
