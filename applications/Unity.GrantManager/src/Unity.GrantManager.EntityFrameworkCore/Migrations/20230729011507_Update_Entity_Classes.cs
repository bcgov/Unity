using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEntityClasses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UnityIntake_UnityGrantPrograms_GrantProgramId",
                table: "UnityIntake");

            migrationBuilder.DropIndex(
                name: "IX_UnityIntake_GrantProgramId",
                table: "UnityIntake");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UnityGrantPrograms",
                table: "UnityGrantPrograms");

            migrationBuilder.DropColumn(
                name: "GrantProgramId",
                table: "UnityIntake");

            migrationBuilder.RenameTable(
                name: "UnityGrantPrograms",
                newName: "UnityGrantProgram");

            migrationBuilder.RenameIndex(
                name: "IX_UnityGrantPrograms_ProgramName",
                table: "UnityGrantProgram",
                newName: "IX_UnityGrantProgram_ProgramName");

            migrationBuilder.AddColumn<double>(
                name: "Budget",
                table: "UnityIntake",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<DateOnly>(
                name: "EndDate",
                table: "UnityIntake",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<DateOnly>(
                name: "StartDate",
                table: "UnityIntake",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));

            migrationBuilder.AddColumn<string>(
                name: "ChefsApplicationFormGuid",
                table: "UnityApplicationForm",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ChefsCriteriaFormGuid",
                table: "UnityApplicationForm",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<string>(
                name: "ProgramName",
                table: "UnityGrantProgram",
                type: "character varying(250)",
                maxLength: 250,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UnityGrantProgram",
                table: "UnityGrantProgram",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserRoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sub = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "text", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnityAdjudicationAssessment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("PK_UnityAdjudicationAssessment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnityAdjudicationAssessment_UnityApplication_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "UnityApplication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UnityAdjudicationAssessment_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UnityApplicantAgent",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicantId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnityApplicantAgent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnityApplicantAgent_UnityApplicant_ApplicantId",
                        column: x => x.ApplicantId,
                        principalTable: "UnityApplicant",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UnityApplicantAgent_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UnityApplicationAssignment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnityApplicationAssignment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnityApplicationAssignment_UnityApplication_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "UnityApplication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UnityApplicationAssignment_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UnityFormSubmission",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
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
                name: "IX_UnityAdjudicationAssessment_ApplicationId",
                table: "UnityAdjudicationAssessment",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_UnityAdjudicationAssessment_UserId",
                table: "UnityAdjudicationAssessment",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UnityApplicantAgent_ApplicantId",
                table: "UnityApplicantAgent",
                column: "ApplicantId");

            migrationBuilder.CreateIndex(
                name: "IX_UnityApplicantAgent_UserId",
                table: "UnityApplicantAgent",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UnityApplicationAssignment_ApplicationId",
                table: "UnityApplicationAssignment",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_UnityApplicationAssignment_UserId",
                table: "UnityApplicationAssignment",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UnityFormSubmission_ApplicationId",
                table: "UnityFormSubmission",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_UnityFormSubmission_UserId",
                table: "UnityFormSubmission",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UnityAdjudicationAssessment");

            migrationBuilder.DropTable(
                name: "UnityApplicantAgent");

            migrationBuilder.DropTable(
                name: "UnityApplicationAssignment");

            migrationBuilder.DropTable(
                name: "UnityFormSubmission");

            migrationBuilder.DropTable(
                name: "User");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UnityGrantProgram",
                table: "UnityGrantProgram");

            migrationBuilder.DropColumn(
                name: "Budget",
                table: "UnityIntake");

            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "UnityIntake");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "UnityIntake");

            migrationBuilder.DropColumn(
                name: "ChefsApplicationFormGuid",
                table: "UnityApplicationForm");

            migrationBuilder.DropColumn(
                name: "ChefsCriteriaFormGuid",
                table: "UnityApplicationForm");

            migrationBuilder.RenameTable(
                name: "UnityGrantProgram",
                newName: "UnityGrantPrograms");

            migrationBuilder.RenameIndex(
                name: "IX_UnityGrantProgram_ProgramName",
                table: "UnityGrantPrograms",
                newName: "IX_UnityGrantPrograms_ProgramName");

            migrationBuilder.AddColumn<Guid>(
                name: "GrantProgramId",
                table: "UnityIntake",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AlterColumn<string>(
                name: "ProgramName",
                table: "UnityGrantPrograms",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(250)",
                oldMaxLength: 250);

            migrationBuilder.AddPrimaryKey(
                name: "PK_UnityGrantPrograms",
                table: "UnityGrantPrograms",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_UnityIntake_GrantProgramId",
                table: "UnityIntake",
                column: "GrantProgramId");

            migrationBuilder.AddForeignKey(
                name: "FK_UnityIntake_UnityGrantPrograms_GrantProgramId",
                table: "UnityIntake",
                column: "GrantProgramId",
                principalTable: "UnityGrantPrograms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
