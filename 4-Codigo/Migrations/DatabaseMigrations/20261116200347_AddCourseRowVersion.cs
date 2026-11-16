using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lumen.Migrations.DatabaseMigrations
{
    /// <inheritdoc />
    public partial class AddCourseRowVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "RowVersion",
                table: "Courses",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"Courses\" DROP COLUMN \"RowVersion\";");
        }
    }
}
