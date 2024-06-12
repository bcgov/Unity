using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class UpdateDefaultValueEconomicRegion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE public.\"Applications\" SET \"EconomicRegion\" = NULL WHERE \"EconomicRegion\" = '{Region}'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE public.\"Applications\" SET \"EconomicRegion\" = '{Region}' WHERE \"EconomicRegion\" IS NULL");
        }
    }
}
