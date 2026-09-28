using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    public partial class CleanupApplicantTemplateVariables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DELETE FROM ""Notifications"".""TemplateVariables""
WHERE ""TemplateType"" IN ('Application', 'Applicant')
    AND ""Token"" NOT IN ('applicant_name', 'organization_name', 'applicant_id', 'today_date');

UPDATE ""Notifications"".""TemplateVariables""
SET ""Name"" = CASE ""Token""
    WHEN 'applicant_name' THEN 'Applicant name'
    WHEN 'organization_name' THEN 'Registered Organization Name'
    WHEN 'applicant_id' THEN 'Applicant ID'
    WHEN 'today_date' THEN 'Today''s Date'
END,
    ""MapTo"" = CASE
        WHEN ""TemplateType"" = 'Application' AND ""Token"" = 'applicant_name' THEN 'application.applicantName'
        WHEN ""TemplateType"" = 'Application' AND ""Token"" = 'organization_name' THEN 'application.organizationName'
        WHEN ""TemplateType"" = 'Application' AND ""Token"" = 'applicant_id' THEN 'application.unityApplicantId'
        WHEN ""TemplateType"" = 'Applicant' AND ""Token"" = 'applicant_name' THEN 'applicantName'
        WHEN ""TemplateType"" = 'Applicant' AND ""Token"" = 'organization_name' THEN 'orgName'
        WHEN ""TemplateType"" = 'Applicant' AND ""Token"" = 'applicant_id' THEN 'unityApplicantId'
        WHEN ""Token"" = 'today_date' THEN ''
    END
WHERE ""TemplateType"" IN ('Application', 'Applicant')
    AND ""Token"" IN ('applicant_name', 'organization_name', 'applicant_id', 'today_date');");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}