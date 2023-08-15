using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations
{
    /// <inheritdoc />
    public partial class UpdateApplicationAssignmentTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UnityApplicationAssignment");

            migrationBuilder.CreateTable(
                name: "UnityApplicationUserAssignment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false),
                    OidcSub = table.Column<string>(type: "text", nullable: false),
                    ApplicationFormId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssigneeDisplayName = table.Column<string>(type: "text", nullable: false),
                    AssignmentTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ExtraProperties = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnityApplicationUserAssignment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnityApplicationUserAssignment_UnityApplicationForm_Applica~",
                        column: x => x.ApplicationFormId,
                        principalTable: "UnityApplicationForm",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UnityApplicationUserAssignment_UnityApplication_Application~",
                        column: x => x.ApplicationId,
                        principalTable: "UnityApplication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UnityApplicationUserAssignment_UnityTeam_TeamId",
                        column: x => x.TeamId,
                        principalTable: "UnityTeam",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UnityApplicationUserAssignment_UnityUser_OidcSub",
                        column: x => x.OidcSub,
                        principalTable: "UnityUser",
                        principalColumn: "OidcSub",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UnityApplicationUserAssignment_ApplicationFormId",
                table: "UnityApplicationUserAssignment",
                column: "ApplicationFormId");

            migrationBuilder.CreateIndex(
                name: "IX_UnityApplicationUserAssignment_ApplicationId",
                table: "UnityApplicationUserAssignment",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_UnityApplicationUserAssignment_OidcSub",
                table: "UnityApplicationUserAssignment",
                column: "OidcSub");

            migrationBuilder.CreateIndex(
                name: "IX_UnityApplicationUserAssignment_TeamId",
                table: "UnityApplicationUserAssignment",
                column: "TeamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UnityApplicationUserAssignment");

            migrationBuilder.CreateTable(
                name: "UnityApplicationAssignment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationFormId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    OidcSub = table.Column<string>(type: "text", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnityApplicationAssignment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnityApplicationAssignment_UnityApplicationForm_Application~",
                        column: x => x.ApplicationFormId,
                        principalTable: "UnityApplicationForm",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UnityApplicationAssignment_UnityApplication_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "UnityApplication",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UnityApplicationAssignment_UnityTeam_TeamId",
                        column: x => x.TeamId,
                        principalTable: "UnityTeam",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UnityApplicationAssignment_UnityUser_OidcSub",
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
                name: "IX_UnityApplicationAssignment_ApplicationId",
                table: "UnityApplicationAssignment",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_UnityApplicationAssignment_OidcSub",
                table: "UnityApplicationAssignment",
                column: "OidcSub");

            migrationBuilder.CreateIndex(
                name: "IX_UnityApplicationAssignment_TeamId",
                table: "UnityApplicationAssignment",
                column: "TeamId");
        }
    }
}
