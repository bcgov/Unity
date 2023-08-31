using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations
{
    /// <inheritdoc />
    public partial class CommentUpdatesAndAssessment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UnityAssessmentComment_UnityApplicationFormSubmission_Id",
                table: "UnityAssessmentComment");

            migrationBuilder.DropTable(
                name: "UnityAdjudicationAssessment");

            migrationBuilder.RenameColumn(
                name: "ApplicationFormSubmissionId",
                table: "UnityAssessmentComment",
                newName: "AssessmentId");

            migrationBuilder.CreateTable(
                name: "UnityApplicationComment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnityApplicationComment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnityApplicationComment_UnityApplication_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "UnityApplication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UnityAssessment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsComplete = table.Column<bool>(type: "boolean", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnityAssessment", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UnityAssessmentComment_AssessmentId",
                table: "UnityAssessmentComment",
                column: "AssessmentId");

            migrationBuilder.CreateIndex(
                name: "IX_UnityApplicationComment_ApplicationId",
                table: "UnityApplicationComment",
                column: "ApplicationId");

            migrationBuilder.AddForeignKey(
                name: "FK_UnityAssessmentComment_UnityAssessment_AssessmentId",
                table: "UnityAssessmentComment",
                column: "AssessmentId",
                principalTable: "UnityAssessment",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UnityAssessmentComment_UnityAssessment_AssessmentId",
                table: "UnityAssessmentComment");

            migrationBuilder.DropTable(
                name: "UnityApplicationComment");

            migrationBuilder.DropTable(
                name: "UnityAssessment");

            migrationBuilder.DropIndex(
                name: "IX_UnityAssessmentComment_AssessmentId",
                table: "UnityAssessmentComment");

            migrationBuilder.RenameColumn(
                name: "AssessmentId",
                table: "UnityAssessmentComment",
                newName: "ApplicationFormSubmissionId");

            migrationBuilder.CreateTable(
                name: "UnityAdjudicationAssessment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationFormId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChefsSubmissionGuid = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    OidcSub = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnityAdjudicationAssessment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnityAdjudicationAssessment_UnityApplicant_ApplicantId",
                        column: x => x.ApplicantId,
                        principalTable: "UnityApplicant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UnityAdjudicationAssessment_UnityApplicationForm_Applicatio~",
                        column: x => x.ApplicationFormId,
                        principalTable: "UnityApplicationForm",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UnityAdjudicationAssessment_UnityUser_OidcSub",
                        column: x => x.OidcSub,
                        principalTable: "UnityUser",
                        principalColumn: "OidcSub",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UnityAdjudicationAssessment_ApplicantId",
                table: "UnityAdjudicationAssessment",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_UnityAdjudicationAssessment_ApplicationFormId",
                table: "UnityAdjudicationAssessment",
                column: "ApplicationFormId");

            migrationBuilder.CreateIndex(
                name: "IX_UnityAdjudicationAssessment_OidcSub",
                table: "UnityAdjudicationAssessment",
                column: "OidcSub");

            migrationBuilder.AddForeignKey(
                name: "FK_UnityAssessmentComment_UnityApplicationFormSubmission_Id",
                table: "UnityAssessmentComment",
                column: "Id",
                principalTable: "UnityApplicationFormSubmission",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
