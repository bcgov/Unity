using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class UpdateWSLinking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "WorksheetCorrelationId",
                schema: "Flex",
                table: "WorksheetInstances",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "WorksheetCorrelationProvider",
                schema: "Flex",
                table: "WorksheetInstances",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WorksheetCorrelationId",
                schema: "Flex",
                table: "WorksheetInstances");

            migrationBuilder.DropColumn(
                name: "WorksheetCorrelationProvider",
                schema: "Flex",
                table: "WorksheetInstances");
        }
    }
}
