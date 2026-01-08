using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AB30918_PaymentsEmailGroup_SetToStatic : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE ""Notifications"".""EmailGroups""
                SET ""Type"" = 'static',
                    ""LastModificationTime"" = NOW(),
                    ""LastModifierId"" = ""CreatorId""
                WHERE ""Name"" = 'Payments'
                AND ""Type"" = 'dynamic';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE ""Notifications"".""EmailGroups""
                SET ""Type"" = 'dynamic',
                    ""LastModificationTime"" = NOW(),
                    ""LastModifierId"" = ""CreatorId""
                WHERE ""Name"" = 'Payments'
                AND ""Type"" = 'static';
            ");
        }
    }
}
