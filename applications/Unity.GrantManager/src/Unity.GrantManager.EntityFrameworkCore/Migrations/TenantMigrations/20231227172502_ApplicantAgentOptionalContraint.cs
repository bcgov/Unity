using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class ApplicantAgentOptionalContraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicantAgents_Persons_OidcSubUser",
                table: "ApplicantAgents");

            migrationBuilder.AlterColumn<string>(
                name: "OidcSubUser",
                table: "ApplicantAgents",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicantAgents_Persons_OidcSubUser",
                table: "ApplicantAgents",
                column: "OidcSubUser",
                principalTable: "Persons",
                principalColumn: "OidcSub");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicantAgents_Persons_OidcSubUser",
                table: "ApplicantAgents");

            migrationBuilder.AlterColumn<string>(
                name: "OidcSubUser",
                table: "ApplicantAgents",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicantAgents_Persons_OidcSubUser",
                table: "ApplicantAgents",
                column: "OidcSubUser",
                principalTable: "Persons",
                principalColumn: "OidcSub",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
