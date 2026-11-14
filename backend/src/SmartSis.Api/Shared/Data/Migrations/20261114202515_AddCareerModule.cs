using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartSis.Api.Shared.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCareerModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CareerAssessments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    TermId = table.Column<int>(type: "int", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerAssessments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CareerAssessments_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CareerAssessments_Terms_TermId",
                        column: x => x.TermId,
                        principalTable: "Terms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CareerPaths",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerPaths", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CareerQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Text = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerQuestions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CareerTraits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerTraits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CareerRoadmapSteps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CareerPathId = table.Column<int>(type: "int", nullable: false),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CourseId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerRoadmapSteps", x => x.Id);
                    table.CheckConstraint("CK_CareerRoadmapSteps_Order", "[StepOrder] >= 1");
                    table.ForeignKey(
                        name: "FK_CareerRoadmapSteps_CareerPaths_CareerPathId",
                        column: x => x.CareerPathId,
                        principalTable: "CareerPaths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CareerRoadmapSteps_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CareerQuestionOptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    OptionOrder = table.Column<int>(type: "int", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerQuestionOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CareerQuestionOptions_CareerQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "CareerQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CareerAssessmentTraitScores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssessmentId = table.Column<int>(type: "int", nullable: false),
                    TraitId = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    MaxPoints = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerAssessmentTraitScores", x => x.Id);
                    table.CheckConstraint("CK_CareerAssessmentTraitScores_Score", "[Score] BETWEEN 0 AND 100");
                    table.ForeignKey(
                        name: "FK_CareerAssessmentTraitScores_CareerAssessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "CareerAssessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CareerAssessmentTraitScores_CareerTraits_TraitId",
                        column: x => x.TraitId,
                        principalTable: "CareerTraits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CareerPathTraits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CareerPathId = table.Column<int>(type: "int", nullable: false),
                    TraitId = table.Column<int>(type: "int", nullable: false),
                    Weight = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerPathTraits", x => x.Id);
                    table.CheckConstraint("CK_CareerPathTraits_Weight", "[Weight] BETWEEN 1 AND 10");
                    table.ForeignKey(
                        name: "FK_CareerPathTraits_CareerPaths_CareerPathId",
                        column: x => x.CareerPathId,
                        principalTable: "CareerPaths",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CareerPathTraits_CareerTraits_TraitId",
                        column: x => x.TraitId,
                        principalTable: "CareerTraits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CourseTraits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseId = table.Column<int>(type: "int", nullable: false),
                    TraitId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseTraits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourseTraits_CareerTraits_TraitId",
                        column: x => x.TraitId,
                        principalTable: "CareerTraits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CourseTraits_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CareerAssessmentAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssessmentId = table.Column<int>(type: "int", nullable: false),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    OptionId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerAssessmentAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CareerAssessmentAnswers_CareerAssessments_AssessmentId",
                        column: x => x.AssessmentId,
                        principalTable: "CareerAssessments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CareerAssessmentAnswers_CareerQuestionOptions_OptionId",
                        column: x => x.OptionId,
                        principalTable: "CareerQuestionOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CareerAssessmentAnswers_CareerQuestions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "CareerQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CareerOptionTraitPoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OptionId = table.Column<int>(type: "int", nullable: false),
                    TraitId = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerOptionTraitPoints", x => x.Id);
                    table.CheckConstraint("CK_CareerOptionTraitPoints_Points", "[Points] BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_CareerOptionTraitPoints_CareerQuestionOptions_OptionId",
                        column: x => x.OptionId,
                        principalTable: "CareerQuestionOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CareerOptionTraitPoints_CareerTraits_TraitId",
                        column: x => x.TraitId,
                        principalTable: "CareerTraits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CareerAssessmentAnswers_AssessmentId_QuestionId",
                table: "CareerAssessmentAnswers",
                columns: new[] { "AssessmentId", "QuestionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CareerAssessmentAnswers_OptionId",
                table: "CareerAssessmentAnswers",
                column: "OptionId");

            migrationBuilder.CreateIndex(
                name: "IX_CareerAssessmentAnswers_QuestionId",
                table: "CareerAssessmentAnswers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_CareerAssessments_StudentId_TermId",
                table: "CareerAssessments",
                columns: new[] { "StudentId", "TermId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CareerAssessments_TermId",
                table: "CareerAssessments",
                column: "TermId");

            migrationBuilder.CreateIndex(
                name: "IX_CareerAssessmentTraitScores_AssessmentId_TraitId",
                table: "CareerAssessmentTraitScores",
                columns: new[] { "AssessmentId", "TraitId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CareerAssessmentTraitScores_TraitId",
                table: "CareerAssessmentTraitScores",
                column: "TraitId");

            migrationBuilder.CreateIndex(
                name: "IX_CareerOptionTraitPoints_OptionId_TraitId",
                table: "CareerOptionTraitPoints",
                columns: new[] { "OptionId", "TraitId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CareerOptionTraitPoints_TraitId",
                table: "CareerOptionTraitPoints",
                column: "TraitId");

            migrationBuilder.CreateIndex(
                name: "IX_CareerPaths_Name",
                table: "CareerPaths",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CareerPathTraits_CareerPathId_TraitId",
                table: "CareerPathTraits",
                columns: new[] { "CareerPathId", "TraitId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CareerPathTraits_TraitId",
                table: "CareerPathTraits",
                column: "TraitId");

            migrationBuilder.CreateIndex(
                name: "IX_CareerQuestionOptions_QuestionId_OptionOrder",
                table: "CareerQuestionOptions",
                columns: new[] { "QuestionId", "OptionOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_CareerQuestions_IsActive",
                table: "CareerQuestions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_CareerRoadmapSteps_CareerPathId_StepOrder",
                table: "CareerRoadmapSteps",
                columns: new[] { "CareerPathId", "StepOrder" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CareerRoadmapSteps_CourseId",
                table: "CareerRoadmapSteps",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_CareerTraits_Name",
                table: "CareerTraits",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseTraits_CourseId_TraitId",
                table: "CourseTraits",
                columns: new[] { "CourseId", "TraitId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseTraits_TraitId",
                table: "CourseTraits",
                column: "TraitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CareerAssessmentAnswers");

            migrationBuilder.DropTable(
                name: "CareerAssessmentTraitScores");

            migrationBuilder.DropTable(
                name: "CareerOptionTraitPoints");

            migrationBuilder.DropTable(
                name: "CareerPathTraits");

            migrationBuilder.DropTable(
                name: "CareerRoadmapSteps");

            migrationBuilder.DropTable(
                name: "CourseTraits");

            migrationBuilder.DropTable(
                name: "CareerAssessments");

            migrationBuilder.DropTable(
                name: "CareerQuestionOptions");

            migrationBuilder.DropTable(
                name: "CareerPaths");

            migrationBuilder.DropTable(
                name: "CareerTraits");

            migrationBuilder.DropTable(
                name: "CareerQuestions");
        }
    }
}
