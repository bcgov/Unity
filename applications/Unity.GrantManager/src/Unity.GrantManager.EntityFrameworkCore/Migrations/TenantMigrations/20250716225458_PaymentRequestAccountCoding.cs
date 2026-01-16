using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class PaymentRequestAccountCoding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountCodingId",
                table: "PaymentRequests",
                type: "uuid",
                nullable: false,
                schema: "Payments",
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));                 
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                     name: "AccountCodingId",
                     table: "PaymentRequests",
                     schema: "Payments");
        }
    }
}
