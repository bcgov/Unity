using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class SiteBank : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<string>(
                name: "BankAccount",
                schema: "Payments",
                table: "Sites",
                type: "text",
                nullable: true,
                defaultValue: "");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                        name: "BankAccount",
                        table: "Sites",
                        schema: "Payments");
        }
    }
}
