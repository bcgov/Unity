using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class applicantinfotab : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SigningAuthorityBusinessPhone",
                table: "ApplicantAgents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SigningAuthorityCellPhone",
                table: "ApplicantAgents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SigningAuthorityEmail",
                table: "ApplicantAgents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SigningAuthorityFullName",
                table: "ApplicantAgents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SigningAuthorityTitle",
                table: "ApplicantAgents",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SigningAuthorityBusinessPhone",
                table: "ApplicantAgents");

            migrationBuilder.DropColumn(
                name: "SigningAuthorityCellPhone",
                table: "ApplicantAgents");

            migrationBuilder.DropColumn(
                name: "SigningAuthorityEmail",
                table: "ApplicantAgents");

            migrationBuilder.DropColumn(
                name: "SigningAuthorityFullName",
                table: "ApplicantAgents");

            migrationBuilder.DropColumn(
                name: "SigningAuthorityTitle",
                table: "ApplicantAgents");
        }
    }
}
