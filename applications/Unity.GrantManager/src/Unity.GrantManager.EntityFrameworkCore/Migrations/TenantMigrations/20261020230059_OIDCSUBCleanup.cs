using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class OIDCSUBCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"UPDATE ""Persons""
                SET ""OidcSub"" = UPPER(REPLACE(""OidcSub"", '@idir', ''))
                WHERE ""OidcSub"" LIKE '%@idir';");
                
            migrationBuilder.Sql(
                @"UPDATE ""Persons""
                SET ""OidcSub"" = UPPER(REPLACE(""OidcSub"", '@azureidir', ''))
                WHERE ""OidcSub"" LIKE '@azureidir';");
                                
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
