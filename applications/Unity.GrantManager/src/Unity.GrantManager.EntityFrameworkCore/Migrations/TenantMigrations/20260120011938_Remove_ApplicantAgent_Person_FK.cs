using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class Remove_ApplicantAgent_Person_FK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicantAgents_Persons_OidcSubUser",
                table: "ApplicantAgents");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Persons_OidcSub",
                table: "Persons");

            migrationBuilder.DropIndex(
                name: "IX_ApplicantAgents_OidcSubUser",
                table: "ApplicantAgents");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddUniqueConstraint(
                name: "AK_Persons_OidcSub",
                table: "Persons",
                column: "OidcSub");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantAgents_OidcSubUser",
                table: "ApplicantAgents",
                column: "OidcSubUser");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicantAgents_Persons_OidcSubUser",
                table: "ApplicantAgents",
                column: "OidcSubUser",
                principalTable: "Persons",
                principalColumn: "OidcSub");
        }
    }
}
