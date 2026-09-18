using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lumen.Migrations.DatabaseMigrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "TEXT", maxLength: 254, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Role = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    FirstName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Dni = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Address = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    PostalCode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourseModules",
                columns: table => new
                {
                    RevisionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Order = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    DurationMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    Content = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: false),
                    Resources = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseModules", x => new { x.RevisionId, x.Id });
                });

            migrationBuilder.CreateTable(
                name: "CourseReviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CourseRevisionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Decision = table.Column<string>(type: "TEXT", maxLength: 30, nullable: true),
                    Observation = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    RequestedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReviewedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    RequestedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseReviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseReviews_Users_RequestedByUserId",
                        column: x => x.RequestedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseReviews_Users_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CourseRevisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CourseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Level = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Modality = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    MaxCapacity = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalDurationMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    LearningObjectives = table.Column<string>(type: "TEXT", nullable: false),
                    SuggestedPrerequisites = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseRevisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    OwnerProfessorId = table.Column<Guid>(type: "TEXT", nullable: true),
                    OperationalStatus = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    WorkingRevisionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PublishedRevisionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Courses_CourseRevisions_PublishedRevisionId",
                        column: x => x.PublishedRevisionId,
                        principalTable: "CourseRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Courses_CourseRevisions_WorkingRevisionId",
                        column: x => x.WorkingRevisionId,
                        principalTable: "CourseRevisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Courses_Users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Courses_Users_OwnerProfessorId",
                        column: x => x.OwnerProfessorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Enrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    StudentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CourseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ResolvedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResolvedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CanceledAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CanceledByUserId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Enrollments_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrollments_Users_CanceledByUserId",
                        column: x => x.CanceledByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrollments_Users_ResolvedByUserId",
                        column: x => x.ResolvedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrollments_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EnrollmentModuleCompletions",
                columns: table => new
                {
                    EnrollmentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ModuleId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnrollmentModuleCompletions", x => new { x.EnrollmentId, x.ModuleId });
                    table.ForeignKey(
                        name: "FK_EnrollmentModuleCompletions_Enrollments_EnrollmentId",
                        column: x => x.EnrollmentId,
                        principalTable: "Enrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CourseModules_RevisionId_Order",
                table: "CourseModules",
                columns: new[] { "RevisionId", "Order" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseReviews_CourseRevisionId",
                table: "CourseReviews",
                column: "CourseRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseReviews_Decision",
                table: "CourseReviews",
                column: "Decision");

            migrationBuilder.CreateIndex(
                name: "IX_CourseReviews_RequestedAtUtc",
                table: "CourseReviews",
                column: "RequestedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_CourseReviews_RequestedByUserId",
                table: "CourseReviews",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseReviews_ReviewedByUserId",
                table: "CourseReviews",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_CourseRevisions_CourseId_Version",
                table: "CourseRevisions",
                columns: new[] { "CourseId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseRevisions_Status",
                table: "CourseRevisions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_CreatedAtUtc",
                table: "Courses",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_CreatedByUserId",
                table: "Courses",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_OperationalStatus",
                table: "Courses",
                column: "OperationalStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_OwnerProfessorId",
                table: "Courses",
                column: "OwnerProfessorId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_PublishedRevisionId",
                table: "Courses",
                column: "PublishedRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_WorkingRevisionId",
                table: "Courses",
                column: "WorkingRevisionId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_CanceledByUserId",
                table: "Enrollments",
                column: "CanceledByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_CourseId",
                table: "Enrollments",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_RequestedAtUtc",
                table: "Enrollments",
                column: "RequestedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_ResolvedByUserId",
                table: "Enrollments",
                column: "ResolvedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Enrollments_StudentId_CourseId_Status",
                table: "Enrollments",
                columns: new[] { "StudentId", "CourseId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Users_CreatedAtUtc",
                table: "Users",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Users_NormalizedEmail",
                table: "Users",
                column: "NormalizedEmail",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Role",
                table: "Users",
                column: "Role");

            migrationBuilder.AddForeignKey(
                name: "FK_CourseModules_CourseRevisions_RevisionId",
                table: "CourseModules",
                column: "RevisionId",
                principalTable: "CourseRevisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseReviews_CourseRevisions_CourseRevisionId",
                table: "CourseReviews",
                column: "CourseRevisionId",
                principalTable: "CourseRevisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CourseRevisions_Courses_CourseId",
                table: "CourseRevisions",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Courses_CourseRevisions_PublishedRevisionId",
                table: "Courses");

            migrationBuilder.DropForeignKey(
                name: "FK_Courses_CourseRevisions_WorkingRevisionId",
                table: "Courses");

            migrationBuilder.DropTable(
                name: "CourseModules");

            migrationBuilder.DropTable(
                name: "CourseReviews");

            migrationBuilder.DropTable(
                name: "EnrollmentModuleCompletions");

            migrationBuilder.DropTable(
                name: "Enrollments");

            migrationBuilder.DropTable(
                name: "CourseRevisions");

            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
