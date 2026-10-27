using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.HostMigrations
{
    /// <inheritdoc />
    public partial class OIDCSUBCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                @"UPDATE ""Users""
                SET ""OidcSub"" = UPPER(REPLACE(""OidcSub"", '@idir', ''))
                WHERE ""OidcSub"" LIKE '%@idir';");    

            migrationBuilder.Sql(
                @"UPDATE ""Users""
                SET ""OidcSub"" = UPPER(REPLACE(""OidcSub"", '@azureidir', ''))
                WHERE ""OidcSub"" LIKE '@azureidir';");                             
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
