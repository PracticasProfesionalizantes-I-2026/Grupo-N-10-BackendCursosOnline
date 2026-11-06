using Lumen.DataAccess;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Lumen.Migrations.DatabaseMigrations;

[DbContext(typeof(LumenDbContext))]
[Migration("20261106220000_PreventReviewSnapshotReplacement")]
public sealed class PreventReviewSnapshotReplacement : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TRIGGER TR_CourseReviewSnapshots_PreventReplacement
            BEFORE INSERT ON CourseReviewSnapshots
            WHEN EXISTS (SELECT 1 FROM CourseReviewSnapshots WHERE ReviewId = NEW.ReviewId)
            BEGIN
                SELECT RAISE(ABORT, 'La evidencia de auditoría ya existe.');
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.Sql("DROP TRIGGER TR_CourseReviewSnapshots_PreventReplacement;");
}
