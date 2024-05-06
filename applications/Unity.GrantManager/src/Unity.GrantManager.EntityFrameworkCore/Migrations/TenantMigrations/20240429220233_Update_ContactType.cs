using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class Update_ContactType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE public.""ApplicationContact""
                SET ""ContactType""='ADDITIONAL_SIGNING_AUTHORITY'
                WHERE ""ContactType""='SIGNING_AUTHORITY';");

            migrationBuilder.Sql(@"
                UPDATE public.""ApplicationContact""
                SET ""ContactType""='ADDITIONAL_CONTACT'
                WHERE ""ContactType""='PRIMARY_CONTACT';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE public.""ApplicationContact""
                SET ""ContactType""='SIGNING_AUTHORITY'
                WHERE ""ContactType""='ADDITIONAL_SIGNING_AUTHORITY';");

            migrationBuilder.Sql(@"
                UPDATE public.""ApplicationContact""
                SET ""ContactType""='PRIMARY_CONTACT'
                WHERE ""ContactType""='ADDITIONAL_CONTACT';");
        }
    }
}
