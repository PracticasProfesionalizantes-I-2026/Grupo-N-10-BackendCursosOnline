using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lumen.Migrations.DatabaseMigrations
{
    /// <inheritdoc />
    public partial class EnforceSingleActiveEnrollment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_StudentId_CourseId_Active",
                table: "Enrollments",
                columns: new[] { "StudentId", "CourseId" },
                unique: true,
                filter: "\"Status\" IN ('Pendiente', 'Aprobada')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Enrollments_StudentId_CourseId_Active",
                table: "Enrollments");
        }
    }
}
