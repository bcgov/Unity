using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class Fix_SupplierLastUpdatedInCAS_Infinity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE ""Payments"".""Suppliers""
                SET ""LastUpdatedInCAS"" = NULL
                WHERE ""LastUpdatedInCAS"" = '-infinity';

                UPDATE ""Payments"".""Sites""
                SET ""LastUpdatedInCas"" = NULL
                WHERE ""LastUpdatedInCas"" = '-infinity';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Cannot restore original missing dates — intentional no-op
        }
    }
}
