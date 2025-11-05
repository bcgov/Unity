using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AB29680_NonRegBusinessName_Datafix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE public.""Applicants""
                SET ""NonRegisteredBusinessName"" = ""NonRegOrgName""
                WHERE 
                    (COALESCE(TRIM(""NonRegOrgName""), '') <> '') AND
                    COALESCE(TRIM(""NonRegisteredBusinessName""), '') = '';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE public.""Applicants""
                SET ""NonRegisteredBusinessName"" = NULL
                WHERE ""NonRegisteredBusinessName"" = ""NonRegOrgName"";
            ");
        }
    }
}
