using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AB33234_ExternalLinksApplicantMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Existing rows store ExternalLinks as a bare JSON array. Wrap them in the new
            // { ApplicantMessage, Links } object shape expected by the current mapping.
            migrationBuilder.Sql(
                """
                UPDATE "ApplicationForms"
                SET "ExternalLinks" = jsonb_build_object('ApplicantMessage', '', 'Links', "ExternalLinks")
                WHERE jsonb_typeof("ExternalLinks") = 'array';
                """);

            migrationBuilder.AlterColumn<string>(
                name: "ExternalLinks",
                table: "ApplicationForms",
                type: "jsonb",
                nullable: false,
                defaultValue: "{\"ApplicantMessage\":\"\",\"Links\":[]}",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldDefaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ExternalLinks",
                table: "ApplicationForms",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldDefaultValue: "{\"ApplicantMessage\":\"\",\"Links\":[]}");

            migrationBuilder.Sql(
                """
                UPDATE "ApplicationForms"
                SET "ExternalLinks" = COALESCE("ExternalLinks" -> 'Links', '[]'::jsonb)
                WHERE jsonb_typeof("ExternalLinks") = 'object';
                """);
        }
    }
}
