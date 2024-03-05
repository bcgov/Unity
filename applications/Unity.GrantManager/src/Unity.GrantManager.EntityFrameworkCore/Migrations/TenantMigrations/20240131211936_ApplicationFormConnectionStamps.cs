using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class ApplicationFormConnectionStamps : Migration
    {
       /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AttemptedConnectionDate",
                table: "ApplicationForms",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConnectionHttpStatus",
                table: "ApplicationForms",
                type: "text",
                nullable: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttemptedConnectionDate",
                table: "ApplicationForms");

            migrationBuilder.DropColumn(
                name: "ConnectionHttpStatus",
                table: "ApplicationForms");
        }
    }
}
