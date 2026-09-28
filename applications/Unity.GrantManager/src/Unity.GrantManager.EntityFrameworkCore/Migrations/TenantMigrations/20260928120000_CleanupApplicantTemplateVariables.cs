using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Unity.GrantManager.EntityFrameworkCore;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    [DbContext(typeof(GrantTenantDbContext))]
    [Migration("20260928120000_CleanupApplicantTemplateVariables")]
    public partial class CleanupApplicantTemplateVariables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
TRUNCATE TABLE ""Notifications"".""TemplateVariables"";");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}