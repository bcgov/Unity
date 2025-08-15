using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class DropPaymentColumns : Migration
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
                    AND column_name = 'MinistryClient'
                    ) THEN
                    ALTER TABLE ""Payments"".""PaymentConfigurations"" DROP COLUMN ""MinistryClient"";
                    END IF;

                    IF EXISTS (
                    SELECT 1 FROM information_schema.columns 
                    WHERE table_schema = 'Payments' 
                    AND table_name = 'PaymentConfigurations' 
                    AND column_name = 'Responsibility'
                    ) THEN
                    ALTER TABLE ""Payments"".""PaymentConfigurations"" DROP COLUMN ""Responsibility"";
                    END IF;

                    IF EXISTS (
                    SELECT 1 FROM information_schema.columns 
                    WHERE table_schema = 'Payments' 
                    AND table_name = 'PaymentConfigurations' 
                    AND column_name = 'Stob'
                    ) THEN
                    ALTER TABLE ""Payments"".""PaymentConfigurations"" DROP COLUMN ""Stob"";
                    END IF;

                    IF EXISTS (
                    SELECT 1 FROM information_schema.columns 
                    WHERE table_schema = 'Payments' 
                    AND table_name = 'PaymentConfigurations' 
                    AND column_name = 'ServiceLine'
                    ) THEN
                    ALTER TABLE ""Payments"".""PaymentConfigurations"" DROP COLUMN ""ServiceLine"";
                    END IF;

                    IF EXISTS (
                    SELECT 1 FROM information_schema.columns 
                    WHERE table_schema = 'Payments' 
                    AND table_name = 'PaymentConfigurations' 
                    AND column_name = 'ProjectNumber'
                    ) THEN
                    ALTER TABLE ""Payments"".""PaymentConfigurations"" DROP COLUMN ""ProjectNumber"";
                    END IF;
                END
                $$ LANGUAGE plpgsql;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
