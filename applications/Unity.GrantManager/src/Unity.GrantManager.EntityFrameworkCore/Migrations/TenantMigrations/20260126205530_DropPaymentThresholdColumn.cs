using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class DropPaymentThresholdColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'Payments'
                        AND table_name = 'PaymentConfigurations'
                        AND column_name = 'PaymentThreshold'
                    ) THEN
                        ALTER TABLE ""Payments"".""PaymentConfigurations"" DROP COLUMN ""PaymentThreshold"";
                    END IF;
                END
                $$ LANGUAGE plpgsql;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PaymentThreshold",
                schema: "Payments",
                table: "PaymentConfigurations",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
