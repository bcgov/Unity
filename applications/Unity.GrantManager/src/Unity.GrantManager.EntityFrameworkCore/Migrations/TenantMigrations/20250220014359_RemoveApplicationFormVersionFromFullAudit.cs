using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class RemoveApplicationFormVersionFromFullAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeleterId",
                table: "ApplicationFormVersion");

            migrationBuilder.DropColumn(
                name: "DeletionTime",
                table: "ApplicationFormVersion");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "ApplicationFormVersion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DeleterId",
                table: "ApplicationFormVersion",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletionTime",
                table: "ApplicationFormVersion",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "ApplicationFormVersion",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
