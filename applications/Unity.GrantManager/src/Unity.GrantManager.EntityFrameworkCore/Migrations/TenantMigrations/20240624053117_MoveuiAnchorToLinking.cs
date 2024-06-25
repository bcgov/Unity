using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class MoveuiAnchorToLinking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UIAnchor",
                schema: "Flex",
                table: "Worksheets");

            migrationBuilder.AddColumn<string>(
                name: "UiAnchor",
                schema: "Flex",
                table: "WorksheetLinks",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UiAnchor",
                schema: "Flex",
                table: "WorksheetLinks");

            migrationBuilder.AddColumn<string>(
                name: "UIAnchor",
                schema: "Flex",
                table: "Worksheets",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
