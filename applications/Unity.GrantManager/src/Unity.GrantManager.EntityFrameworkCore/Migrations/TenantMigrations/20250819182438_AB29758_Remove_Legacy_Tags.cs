using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AB29758_Remove_Legacy_Tags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Text",
                schema: "Payments",
                table: "PaymentTags");

            migrationBuilder.DropColumn(
                name: "Text",
                table: "ApplicationTags");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Text",
                schema: "Payments",
                table: "PaymentTags",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Text",
                table: "ApplicationTags",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
