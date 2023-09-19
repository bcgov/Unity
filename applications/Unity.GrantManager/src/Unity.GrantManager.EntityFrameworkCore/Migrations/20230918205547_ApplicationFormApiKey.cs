using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations
{
    /// <inheritdoc />
    public partial class ApplicationFormApiKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeleterId",
                table: "UnityApplicationForm");

            migrationBuilder.DropColumn(
                name: "DeletionTime",
                table: "UnityApplicationForm");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "UnityApplicationForm");

            migrationBuilder.AddColumn<string>(
                name: "ApiKey",
                table: "UnityApplicationForm",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApiKey",
                table: "UnityApplicationForm");

            migrationBuilder.AddColumn<Guid>(
                name: "DeleterId",
                table: "UnityApplicationForm",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletionTime",
                table: "UnityApplicationForm",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "UnityApplicationForm",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
