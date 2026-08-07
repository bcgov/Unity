using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AB33996_AddApplicantFiscalYearEndRestricted : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "FiscalYearEnd",
                table: "Applicants",
                type: "date",
                nullable: true);

            // Trigger keeps FiscalYearEnd in sync whenever FiscalMonth or FiscalDay change.
            // PostgreSQL generated columns cannot reference volatile functions (e.g. CURRENT_DATE),
            // so a BEFORE trigger is used instead of a GENERATED ALWAYS AS column.
            migrationBuilder.Sql(@"
CREATE OR REPLACE FUNCTION compute_applicants_fiscal_year_end()
RETURNS TRIGGER AS $$
DECLARE
    month_num integer;
BEGIN
    month_num := CASE NEW.""FiscalMonth""
        WHEN 'Jan' THEN 1 WHEN 'Feb' THEN 2 WHEN 'Mar' THEN 3
        WHEN 'Apr' THEN 4 WHEN 'May' THEN 5 WHEN 'Jun' THEN 6
        WHEN 'Jul' THEN 7 WHEN 'Aug' THEN 8 WHEN 'Sep' THEN 9
        WHEN 'Oct' THEN 10 WHEN 'Nov' THEN 11 WHEN 'Dec' THEN 12
        ELSE NULL
    END;

    IF month_num IS NOT NULL AND NEW.""FiscalDay"" IS NOT NULL THEN
        BEGIN
            NEW.""FiscalYearEnd"" := MAKE_DATE(
                EXTRACT(YEAR FROM CURRENT_DATE)::int,
                month_num,
                NEW.""FiscalDay""
            );
        EXCEPTION WHEN others THEN
            NEW.""FiscalYearEnd"" := NULL;
        END;
    ELSE
        NEW.""FiscalYearEnd"" := NULL;
    END IF;

    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_applicants_fiscal_year_end
BEFORE INSERT OR UPDATE OF ""FiscalMonth"", ""FiscalDay""
ON ""Applicants""
FOR EACH ROW EXECUTE FUNCTION compute_applicants_fiscal_year_end();
");

            // Backfill existing rows by touching FiscalDay, which fires the trigger above.
            migrationBuilder.Sql(@"
UPDATE ""Applicants""
SET ""FiscalDay"" = ""FiscalDay""
WHERE ""FiscalMonth"" IS NOT NULL AND ""FiscalDay"" IS NOT NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP TRIGGER IF EXISTS trg_applicants_fiscal_year_end ON ""Applicants"";
DROP FUNCTION IF EXISTS compute_applicants_fiscal_year_end();
");

            migrationBuilder.DropColumn(
                name: "FiscalYearEnd",
                table: "Applicants");
        }
    }
}
