using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations
{
    /// <inheritdoc />
    public partial class RemovOIDC : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UnityApplicationFormSubmission_UnityUser_OidcSub",
                table: "UnityApplicationFormSubmission");

            migrationBuilder.DropIndex(
                name: "IX_UnityApplicationFormSubmission_OidcSub",
                table: "UnityApplicationFormSubmission");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UnityApplicationFormSubmission_OidcSub",
                table: "UnityApplicationFormSubmission",
                column: "OidcSub");

            migrationBuilder.AddForeignKey(
                name: "FK_UnityApplicationFormSubmission_UnityUser_OidcSub",
                table: "UnityApplicationFormSubmission",
                column: "OidcSub",
                principalTable: "UnityUser",
                principalColumn: "OidcSub",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
