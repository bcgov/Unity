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
            // Existing rows store ExternalLinks as a bare JSON array, with the applicant message
            // (if any) saved in the renewal link's (ExternalLinkType 2) Description. Wrap them in
            // the new { ApplicantMessage, Links } object shape, lifting that message out so it
            // isn't silently lost.
            migrationBuilder.Sql(
                """
                UPDATE "ApplicationForms" AS form
                SET "ExternalLinks" = jsonb_build_object(
                    'ApplicantMessage',
                    COALESCE((
                        SELECT link ->> 'Description'
                        FROM jsonb_array_elements(form."ExternalLinks") AS link
                        WHERE link ->> 'ExternalLinkType' = '2'
                        LIMIT 1
                    ), ''),
                    'Links', form."ExternalLinks")
                WHERE jsonb_typeof(form."ExternalLinks") = 'array';
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

            // Push the applicant message back into the renewal link's Description before
            // dropping the wrapper, so a rollback doesn't lose a message edited after the
            // forward migration ran.
            migrationBuilder.Sql(
                """
                UPDATE "ApplicationForms" AS form
                SET "ExternalLinks" = COALESCE((
                    SELECT jsonb_agg(
                        CASE
                            WHEN link ->> 'ExternalLinkType' = '2'
                                THEN jsonb_set(link, '{Description}', to_jsonb(form."ExternalLinks" ->> 'ApplicantMessage'))
                            ELSE link
                        END)
                    FROM jsonb_array_elements(form."ExternalLinks" -> 'Links') AS link
                ), '[]'::jsonb)
                WHERE jsonb_typeof(form."ExternalLinks") = 'object';
                """);
        }
    }
}
