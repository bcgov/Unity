using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFields1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DeleterId",
                table: "UnityIntake",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletionTime",
                table: "UnityIntake",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "UnityIntake",
                type: "boolean",
                nullable: false,
                defaultValue: false);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeleterId",
                table: "UnityIntake");

            migrationBuilder.DropColumn(
                name: "DeletionTime",
                table: "UnityIntake");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "UnityIntake");

            migrationBuilder.DropColumn(
                name: "DeleterId",
                table: "UnityApplicationForm");

            migrationBuilder.DropColumn(
                name: "DeletionTime",
                table: "UnityApplicationForm");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "UnityApplicationForm");
        }
    }
}
