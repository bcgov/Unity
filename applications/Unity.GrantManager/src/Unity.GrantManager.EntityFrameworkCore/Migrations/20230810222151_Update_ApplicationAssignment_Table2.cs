using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations
{
    /// <inheritdoc />
    public partial class UpdateApplicationAssignmentTable2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UnityApplicationUserAssignment_UnityTeam_TeamId",
                table: "UnityApplicationUserAssignment");

            migrationBuilder.DropForeignKey(
                name: "FK_UnityApplicationUserAssignment_UnityUser_OidcSub",
                table: "UnityApplicationUserAssignment");

            migrationBuilder.DropIndex(
                name: "IX_UnityApplicationUserAssignment_OidcSub",
                table: "UnityApplicationUserAssignment");

            migrationBuilder.DropIndex(
                name: "IX_UnityApplicationUserAssignment_TeamId",
                table: "UnityApplicationUserAssignment");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UnityApplicationUserAssignment_OidcSub",
                table: "UnityApplicationUserAssignment",
                column: "OidcSub");

            migrationBuilder.CreateIndex(
                name: "IX_UnityApplicationUserAssignment_TeamId",
                table: "UnityApplicationUserAssignment",
                column: "TeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_UnityApplicationUserAssignment_UnityTeam_TeamId",
                table: "UnityApplicationUserAssignment",
                column: "TeamId",
                principalTable: "UnityTeam",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UnityApplicationUserAssignment_UnityUser_OidcSub",
                table: "UnityApplicationUserAssignment",
                column: "OidcSub",
                principalTable: "UnityUser",
                principalColumn: "OidcSub",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
