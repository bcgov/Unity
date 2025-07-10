using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class UpdateCustomFieldValueBaseType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeleterId",
                schema: "Flex",
                table: "CustomFieldValues");

            migrationBuilder.DropColumn(
                name: "DeletionTime",
                schema: "Flex",
                table: "CustomFieldValues");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                schema: "Flex",
                table: "CustomFieldValues");          
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DeleterId",
                schema: "Flex",
                table: "CustomFieldValues",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletionTime",
                schema: "Flex",
                table: "CustomFieldValues",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                schema: "Flex",
                table: "CustomFieldValues",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
