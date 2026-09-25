using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AB34344_RemoveAutoReportingViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            /*
             * Removes the deprecated auto-generated reporting views (Form-*, Worksheet-*,
             * Scoresheet-*) and everything that fed them. Explicitly configured views
             * (Reporting.ReportColumnsMaps) read source data directly and are unaffected.
             *
             * Every auto view selects "ReportData", so the views to drop are found through
             * their column dependency rather than by name. They must go before the columns
             * are dropped. No CASCADE: if anything was built on top of an auto view the
             * migration fails here instead of silently dropping it.
             */
            migrationBuilder.Sql("""
                DO $$
                DECLARE
                    auto_view regclass;
                BEGIN
                    FOR auto_view IN
                        SELECT DISTINCT r.ev_class::regclass
                        FROM pg_depend d
                        JOIN pg_rewrite r ON r.oid = d.objid
                        JOIN pg_class v ON v.oid = r.ev_class
                        JOIN pg_attribute a ON a.attrelid = d.refobjid AND a.attnum = d.refobjsubid
                        WHERE d.classid = 'pg_rewrite'::regclass
                          AND d.refclassid = 'pg_class'::regclass
                          AND d.refobjid IN (
                              'public."ApplicationFormSubmissions"'::regclass,
                              '"Flex"."WorksheetInstances"'::regclass,
                              '"Flex"."ScoresheetInstances"'::regclass)
                          AND a.attname = 'ReportData'
                          AND v.relkind = 'v'
                    LOOP
                        EXECUTE format('DROP VIEW IF EXISTS %s', auto_view);
                    END LOOP;
                END $$;
                """);

            migrationBuilder.Sql(@"DROP PROCEDURE IF EXISTS ""Reporting"".generate_submissions_view(uuid);");
            migrationBuilder.Sql(@"DROP PROCEDURE IF EXISTS ""Reporting"".generate_worksheets_view(uuid);");
            migrationBuilder.Sql(@"DROP PROCEDURE IF EXISTS ""Reporting"".generate_scoresheets_view(uuid);");

            migrationBuilder.DropColumn(
                name: "ReportColumns",
                schema: "Flex",
                table: "Worksheets");

            migrationBuilder.DropColumn(
                name: "ReportKeys",
                schema: "Flex",
                table: "Worksheets");

            migrationBuilder.DropColumn(
                name: "ReportViewName",
                schema: "Flex",
                table: "Worksheets");

            migrationBuilder.DropColumn(
                name: "ReportData",
                schema: "Flex",
                table: "WorksheetInstances");

            migrationBuilder.DropColumn(
                name: "ReportColumns",
                schema: "Flex",
                table: "Scoresheets");

            migrationBuilder.DropColumn(
                name: "ReportKeys",
                schema: "Flex",
                table: "Scoresheets");

            migrationBuilder.DropColumn(
                name: "ReportViewName",
                schema: "Flex",
                table: "Scoresheets");

            migrationBuilder.DropColumn(
                name: "ReportData",
                schema: "Flex",
                table: "ScoresheetInstances");

            migrationBuilder.DropColumn(
                name: "ReportColumns",
                table: "ApplicationFormVersion");

            migrationBuilder.DropColumn(
                name: "ReportKeys",
                table: "ApplicationFormVersion");

            migrationBuilder.DropColumn(
                name: "ReportViewName",
                table: "ApplicationFormVersion");

            migrationBuilder.DropColumn(
                name: "ReportData",
                table: "ApplicationFormSubmissions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restores the columns only. The dropped data, the auto views, and the
            // generate_*_view procedures are not recoverable from this migration.
            migrationBuilder.AddColumn<string>(
                name: "ReportColumns",
                schema: "Flex",
                table: "Worksheets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReportKeys",
                schema: "Flex",
                table: "Worksheets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReportViewName",
                schema: "Flex",
                table: "Worksheets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReportData",
                schema: "Flex",
                table: "WorksheetInstances",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<string>(
                name: "ReportColumns",
                schema: "Flex",
                table: "Scoresheets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReportKeys",
                schema: "Flex",
                table: "Scoresheets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReportViewName",
                schema: "Flex",
                table: "Scoresheets",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReportData",
                schema: "Flex",
                table: "ScoresheetInstances",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<string>(
                name: "ReportColumns",
                table: "ApplicationFormVersion",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReportKeys",
                table: "ApplicationFormVersion",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReportViewName",
                table: "ApplicationFormVersion",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReportData",
                table: "ApplicationFormSubmissions",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");
        }
    }
}
