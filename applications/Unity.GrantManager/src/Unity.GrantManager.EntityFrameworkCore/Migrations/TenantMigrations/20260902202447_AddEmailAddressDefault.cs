using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AddEmailAddressDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                schema: "Notifications",
                table: "EmailAddressConfigurations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                WITH ranked AS (
                    SELECT "Id", ROW_NUMBER() OVER (
                        PARTITION BY "TenantId"
                        ORDER BY ("EmailType" = 'Sender' AND "IsActive") DESC,
                                 "IsActive" DESC,
                                 "CreationTime",
                                 "Id"
                    ) AS row_number
                    FROM "Notifications"."EmailAddressConfigurations"
                )
                UPDATE "Notifications"."EmailAddressConfigurations" AS configuration
                SET "IsDefault" = true
                FROM ranked
                WHERE configuration."Id" = ranked."Id"
                  AND ranked.row_number = 1;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_EmailAddressConfigurations_TenantId_IsDefault",
                schema: "Notifications",
                table: "EmailAddressConfigurations",
                column: "TenantId",
                unique: true,
                filter: "\"IsDefault\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmailAddressConfigurations_TenantId_IsDefault",
                schema: "Notifications",
                table: "EmailAddressConfigurations");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                schema: "Notifications",
                table: "EmailAddressConfigurations");
        }
    }
}
