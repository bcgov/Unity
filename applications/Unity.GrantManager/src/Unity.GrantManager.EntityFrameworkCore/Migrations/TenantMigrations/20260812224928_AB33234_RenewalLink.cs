using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AB33234_RenewalLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "PublishRenewalLink",
                table: "ApplicationForms",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RenewalLink_Description",
                table: "ApplicationForms",
                type: "character varying(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RenewalLink_Title",
                table: "ApplicationForms",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RenewalLink_Uri",
                table: "ApplicationForms",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PublishRenewalLink",
                table: "ApplicationForms");

            migrationBuilder.DropColumn(
                name: "RenewalLink_Description",
                table: "ApplicationForms");

            migrationBuilder.DropColumn(
                name: "RenewalLink_Title",
                table: "ApplicationForms");

            migrationBuilder.DropColumn(
                name: "RenewalLink_Uri",
                table: "ApplicationForms");
        }
    }
}
