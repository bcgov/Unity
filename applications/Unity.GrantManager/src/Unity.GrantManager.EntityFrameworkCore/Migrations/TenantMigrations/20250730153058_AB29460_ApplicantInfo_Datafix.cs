using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AB29460_ApplicantInfo_Datafix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE public.""Applicants"" AS app
                SET ""ApplicantName"" = LEFT(sub.""MappedApplicantName"", 600)
                FROM (
                    SELECT
                        afs.""ApplicantId"",
                        afs.""Submission""->'submission'->'submission'->'data'->>
                            (afv.""SubmissionHeaderMapping""::json->>'ApplicantName') AS ""MappedApplicantName""
                    FROM
                        public.""ApplicationFormVersion"" AS afv
                    JOIN
                        public.""ApplicationFormSubmissions"" AS afs
                        ON afv.""Id"" = afs.""ApplicationFormVersionId""
                ) AS sub
                WHERE
                    app.""Id"" = sub.""ApplicantId""
                    AND (
                        app.""ApplicantName"" IS NULL
                        OR TRIM(app.""ApplicantName"") = ''
                    )
                    AND sub.""MappedApplicantName"" IS NOT NULL
                    AND TRIM(sub.""MappedApplicantName"") <> '';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
