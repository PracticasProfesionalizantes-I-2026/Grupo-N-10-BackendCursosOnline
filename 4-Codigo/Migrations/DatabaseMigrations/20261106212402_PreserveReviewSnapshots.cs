using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lumen.Migrations.DatabaseMigrations
{
    /// <inheritdoc />
    public partial class PreserveReviewSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CourseReviewSnapshots",
                columns: table => new
                {
                    ReviewId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContentJson = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseReviewSnapshots", x => x.ReviewId);
                    table.ForeignKey(
                        name: "FK_CourseReviewSnapshots_CourseReviews_ReviewId",
                        column: x => x.ReviewId,
                        principalTable: "CourseReviews",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });
            migrationBuilder.Sql("""
                CREATE TRIGGER TR_CourseReviewSnapshots_Immutable
                BEFORE UPDATE ON CourseReviewSnapshots
                BEGIN SELECT RAISE(ABORT, 'El contenido enviado a auditoría es inmutable.'); END;
                """);
            migrationBuilder.Sql("""
                CREATE TRIGGER TR_CourseReviewSnapshots_Preserve
                BEFORE DELETE ON CourseReviewSnapshots
                BEGIN SELECT RAISE(ABORT, 'La evidencia de auditoría debe conservarse.'); END;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("El esquema es aditivo. Revertirlo eliminaría evidencia de auditoría.");
        }
    }
}
