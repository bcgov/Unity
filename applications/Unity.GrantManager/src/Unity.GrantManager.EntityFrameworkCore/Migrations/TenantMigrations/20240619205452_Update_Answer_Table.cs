using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class Update_Answer_Table : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentScore",
                schema: "Flex",
                table: "Answers");

            migrationBuilder.DropColumn(
                name: "DefaultScore",
                schema: "Flex",
                table: "Answers");

            migrationBuilder.DropColumn(
                name: "DefaultValue",
                schema: "Flex",
                table: "Answers");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "CurrentScore",
                schema: "Flex",
                table: "Answers",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "DefaultScore",
                schema: "Flex",
                table: "Answers",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "DefaultValue",
                schema: "Flex",
                table: "Answers",
                type: "jsonb",
                nullable: true);
        }
    }
}
