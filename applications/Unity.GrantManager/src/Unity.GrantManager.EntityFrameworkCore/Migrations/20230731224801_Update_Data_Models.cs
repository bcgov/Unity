using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations
{
    /// <inheritdoc />
    public partial class UpdateDataModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UnityAdjudicationAssessment_UnityApplication_ApplicationId",
                table: "UnityAdjudicationAssessment");

            migrationBuilder.DropForeignKey(
                name: "FK_UnityAdjudicationAssessment_User_UserId",
                table: "UnityAdjudicationAssessment");

            migrationBuilder.DropForeignKey(
                name: "FK_UnityApplicantAgent_User_UserId",
                table: "UnityApplicantAgent");

            migrationBuilder.DropForeignKey(
                name: "FK_UnityApplicationAssignment_User_UserId",
                table: "UnityApplicationAssignment");

            migrationBuilder.DropTable(
                name: "UnityFormSubmission");

            migrationBuilder.DropIndex(
                name: "IX_UnityApplicantAgent_UserId",
                table: "UnityApplicantAgent");

            migrationBuilder.DropPrimaryKey(
                name: "PK_User",
                table: "User");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "UnityApplicantAgent");

            migrationBuilder.DropColumn(
                name: "UserRoleId",
                table: "User");

            migrationBuilder.RenameTable(
                name: "User",
                newName: "UnityUser");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "UnityApplicationAssignment",
                newName: "TeamId");

            migrationBuilder.RenameIndex(
                name: "IX_UnityApplicationAssignment_UserId",
                table: "UnityApplicationAssignment",
                newName: "IX_UnityApplicationAssignment_TeamId");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "UnityAdjudicationAssessment",
                newName: "ApplicationFormId");

            migrationBuilder.RenameColumn(
                name: "ApplicationId",
                table: "UnityAdjudicationAssessment",
                newName: "ApplicantId");

            migrationBuilder.RenameIndex(
                name: "IX_UnityAdjudicationAssessment_UserId",
                table: "UnityAdjudicationAssessment",
                newName: "IX_UnityAdjudicationAssessment_ApplicationFormId");

            migrationBuilder.RenameIndex(
                name: "IX_UnityAdjudicationAssessment_ApplicationId",
                table: "UnityAdjudicationAssessment",
                newName: "IX_UnityAdjudicationAssessment_ApplicantId");

            migrationBuilder.RenameColumn(
                name: "Sub",
                table: "UnityUser",
                newName: "PreferredLastName");

            migrationBuilder.RenameColumn(
                name: "Email",
                table: "UnityUser",
                newName: "PreferredFirstName");

            migrationBuilder.RenameColumn(
                name: "DisplayName",
                table: "UnityUser",
                newName: "Phone");

            migrationBuilder.AddColumn<Guid>(
                name: "ApplicationFormId",
                table: "UnityApplicationAssignment",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "OidcSub",
                table: "UnityApplicationAssignment",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OidcSubUser",
                table: "UnityApplicantAgent",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "UnityApplicantAgent",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RoleForApplicant",
                table: "UnityApplicantAgent",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OidcSub",
                table: "UnityAdjudicationAssessment",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "ConcurrencyStamp",
                table: "UnityUser",
                type: "character varying(40)",
                maxLength: 40,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LegalName",
                table: "UnityUser",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OidcDisplayName",
                table: "UnityUser",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OidcEmail",
                table: "UnityUser",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OidcSub",
                table: "UnityUser",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_UnityUser_OidcSub",
                table: "UnityUser",
                column: "OidcSub");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UnityUser",
                table: "UnityUser",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "GrantApplications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GrantApplications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnityApplicationFormSubmission",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OidcSub = table.Column<string>(type: "text", nullable: false),
                    ApplicantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationFormId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChefsSubmissionGuid = table.Column<string>(type: "text", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnityApplicationFormSubmission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnityApplicationFormSubmission_UnityApplicant_ApplicantId",
                        column: x => x.ApplicantId,
                        principalTable: "UnityApplicant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UnityApplicationFormSubmission_UnityApplicationForm_Applica~",
                        column: x => x.ApplicationFormId,
                        principalTable: "UnityApplicationForm",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UnityApplicationFormSubmission_UnityUser_OidcSub",
                        column: x => x.OidcSub,
                        principalTable: "UnityUser",
                        principalColumn: "OidcSub",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UnityTeam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnityTeam", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnityUserTeam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    OidcSub = table.Column<string>(type: "text", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnityUserTeam", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnityUserTeam_UnityTeam_TeamId",
                        column: x => x.TeamId,
                        principalTable: "UnityTeam",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UnityUserTeam_UnityUser_OidcSub",
                        column: x => x.OidcSub,
                        principalTable: "UnityUser",
                        principalColumn: "OidcSub",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UnityApplicationAssignment_ApplicationFormId",
                table: "UnityApplicationAssignment",
                column: "ApplicationFormId");

            migrationBuilder.CreateIndex(
                name: "IX_UnityApplicationAssignment_OidcSub",
                table: "UnityApplicationAssignment",
                column: "OidcSub");

            migrationBuilder.CreateIndex(
                name: "IX_UnityApplicantAgent_OidcSubUser",
                table: "UnityApplicantAgent",
                column: "OidcSubUser");

            migrationBuilder.CreateIndex(
                name: "IX_UnityAdjudicationAssessment_OidcSub",
                table: "UnityAdjudicationAssessment",
                column: "OidcSub");

            migrationBuilder.CreateIndex(
                name: "IX_UnityUser_OidcSub",
                table: "UnityUser",
                column: "OidcSub");

            migrationBuilder.CreateIndex(
                name: "IX_UnityApplicationFormSubmission_ApplicantId",
                table: "UnityApplicationFormSubmission",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_UnityApplicationFormSubmission_ApplicationFormId",
                table: "UnityApplicationFormSubmission",
                column: "ApplicationFormId");

            migrationBuilder.CreateIndex(
                name: "IX_UnityApplicationFormSubmission_OidcSub",
                table: "UnityApplicationFormSubmission",
                column: "OidcSub");

            migrationBuilder.CreateIndex(
                name: "IX_UnityUserTeam_OidcSub",
                table: "UnityUserTeam",
                column: "OidcSub");

            migrationBuilder.CreateIndex(
                name: "IX_UnityUserTeam_TeamId",
                table: "UnityUserTeam",
                column: "TeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_UnityAdjudicationAssessment_UnityApplicant_ApplicantId",
                table: "UnityAdjudicationAssessment",
                column: "ApplicantId",
                principalTable: "UnityApplicant",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UnityAdjudicationAssessment_UnityApplicationForm_Applicatio~",
                table: "UnityAdjudicationAssessment",
                column: "ApplicationFormId",
                principalTable: "UnityApplicationForm",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UnityAdjudicationAssessment_UnityUser_OidcSub",
                table: "UnityAdjudicationAssessment",
                column: "OidcSub",
                principalTable: "UnityUser",
                principalColumn: "OidcSub",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UnityApplicantAgent_UnityUser_OidcSubUser",
                table: "UnityApplicantAgent",
                column: "OidcSubUser",
                principalTable: "UnityUser",
                principalColumn: "OidcSub",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UnityApplicationAssignment_UnityApplicationForm_Application~",
                table: "UnityApplicationAssignment",
                column: "ApplicationFormId",
                principalTable: "UnityApplicationForm",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UnityApplicationAssignment_UnityTeam_TeamId",
                table: "UnityApplicationAssignment",
                column: "TeamId",
                principalTable: "UnityTeam",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UnityApplicationAssignment_UnityUser_OidcSub",
                table: "UnityApplicationAssignment",
                column: "OidcSub",
                principalTable: "UnityUser",
                principalColumn: "OidcSub",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UnityAdjudicationAssessment_UnityApplicant_ApplicantId",
                table: "UnityAdjudicationAssessment");

            migrationBuilder.DropForeignKey(
                name: "FK_UnityAdjudicationAssessment_UnityApplicationForm_Applicatio~",
                table: "UnityAdjudicationAssessment");

            migrationBuilder.DropForeignKey(
                name: "FK_UnityAdjudicationAssessment_UnityUser_OidcSub",
                table: "UnityAdjudicationAssessment");

            migrationBuilder.DropForeignKey(
                name: "FK_UnityApplicantAgent_UnityUser_OidcSubUser",
                table: "UnityApplicantAgent");

            migrationBuilder.DropForeignKey(
                name: "FK_UnityApplicationAssignment_UnityApplicationForm_Application~",
                table: "UnityApplicationAssignment");

            migrationBuilder.DropForeignKey(
                name: "FK_UnityApplicationAssignment_UnityTeam_TeamId",
                table: "UnityApplicationAssignment");

            migrationBuilder.DropForeignKey(
                name: "FK_UnityApplicationAssignment_UnityUser_OidcSub",
                table: "UnityApplicationAssignment");

            migrationBuilder.DropTable(
                name: "GrantApplications");

            migrationBuilder.DropTable(
                name: "UnityApplicationFormSubmission");

            migrationBuilder.DropTable(
                name: "UnityUserTeam");

            migrationBuilder.DropTable(
                name: "UnityTeam");

            migrationBuilder.DropIndex(
                name: "IX_UnityApplicationAssignment_ApplicationFormId",
                table: "UnityApplicationAssignment");

            migrationBuilder.DropIndex(
                name: "IX_UnityApplicationAssignment_OidcSub",
                table: "UnityApplicationAssignment");

            migrationBuilder.DropIndex(
                name: "IX_UnityApplicantAgent_OidcSubUser",
                table: "UnityApplicantAgent");

            migrationBuilder.DropIndex(
                name: "IX_UnityAdjudicationAssessment_OidcSub",
                table: "UnityAdjudicationAssessment");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_UnityUser_OidcSub",
                table: "UnityUser");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UnityUser",
                table: "UnityUser");

            migrationBuilder.DropIndex(
                name: "IX_UnityUser_OidcSub",
                table: "UnityUser");

            migrationBuilder.DropColumn(
                name: "ApplicationFormId",
                table: "UnityApplicationAssignment");

            migrationBuilder.DropColumn(
                name: "OidcSub",
                table: "UnityApplicationAssignment");

            migrationBuilder.DropColumn(
                name: "OidcSubUser",
                table: "UnityApplicantAgent");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "UnityApplicantAgent");

            migrationBuilder.DropColumn(
                name: "RoleForApplicant",
                table: "UnityApplicantAgent");

            migrationBuilder.DropColumn(
                name: "OidcSub",
                table: "UnityAdjudicationAssessment");

            migrationBuilder.DropColumn(
                name: "LegalName",
                table: "UnityUser");

            migrationBuilder.DropColumn(
                name: "OidcDisplayName",
                table: "UnityUser");

            migrationBuilder.DropColumn(
                name: "OidcEmail",
                table: "UnityUser");

            migrationBuilder.DropColumn(
                name: "OidcSub",
                table: "UnityUser");

            migrationBuilder.RenameTable(
                name: "UnityUser",
                newName: "User");

            migrationBuilder.RenameColumn(
                name: "TeamId",
                table: "UnityApplicationAssignment",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_UnityApplicationAssignment_TeamId",
                table: "UnityApplicationAssignment",
                newName: "IX_UnityApplicationAssignment_UserId");

            migrationBuilder.RenameColumn(
                name: "ApplicationFormId",
                table: "UnityAdjudicationAssessment",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "ApplicantId",
                table: "UnityAdjudicationAssessment",
                newName: "ApplicationId");

            migrationBuilder.RenameIndex(
                name: "IX_UnityAdjudicationAssessment_ApplicationFormId",
                table: "UnityAdjudicationAssessment",
                newName: "IX_UnityAdjudicationAssessment_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_UnityAdjudicationAssessment_ApplicantId",
                table: "UnityAdjudicationAssessment",
                newName: "IX_UnityAdjudicationAssessment_ApplicationId");

            migrationBuilder.RenameColumn(
                name: "PreferredLastName",
                table: "User",
                newName: "Sub");

            migrationBuilder.RenameColumn(
                name: "PreferredFirstName",
                table: "User",
                newName: "Email");

            migrationBuilder.RenameColumn(
                name: "Phone",
                table: "User",
                newName: "DisplayName");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "UnityApplicantAgent",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "ConcurrencyStamp",
                table: "User",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserRoleId",
                table: "User",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddPrimaryKey(
                name: "PK_User",
                table: "User",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "UnityFormSubmission",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChefsSubmissionGuid = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnityFormSubmission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnityFormSubmission_UnityApplication_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "UnityApplication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UnityFormSubmission_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UnityApplicantAgent_UserId",
                table: "UnityApplicantAgent",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UnityFormSubmission_ApplicationId",
                table: "UnityFormSubmission",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_UnityFormSubmission_UserId",
                table: "UnityFormSubmission",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_UnityAdjudicationAssessment_UnityApplication_ApplicationId",
                table: "UnityAdjudicationAssessment",
                column: "ApplicationId",
                principalTable: "UnityApplication",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UnityAdjudicationAssessment_User_UserId",
                table: "UnityAdjudicationAssessment",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UnityApplicantAgent_User_UserId",
                table: "UnityApplicantAgent",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UnityApplicationAssignment_User_UserId",
                table: "UnityApplicationAssignment",
                column: "UserId",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
