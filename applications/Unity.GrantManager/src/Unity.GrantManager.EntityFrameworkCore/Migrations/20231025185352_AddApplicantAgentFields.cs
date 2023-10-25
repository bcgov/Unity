using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicantAgentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ApplicationId",
                table: "UnityApplicantAgent",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "ContactOrder",
                table: "UnityApplicantAgent",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "UnityApplicantAgent",
                type: "character varying(500)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "UnityApplicantAgent",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "UnityApplicantAgent",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "UnityApplicantAgent",
                type: "character varying(40)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Phone2",
                table: "UnityApplicantAgent",
                type: "character varying(40)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "UnityApplicantAgent",
                type: "character varying(40)",
                nullable: false,
                defaultValue: "");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplicationId",
                table: "UnityApplicantAgent");

            migrationBuilder.DropColumn(
                name: "ContactOrder",
                table: "UnityApplicantAgent");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "UnityApplicantAgent");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "UnityApplicantAgent");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "UnityApplicantAgent");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "UnityApplicantAgent");

            migrationBuilder.DropColumn(
                name: "Phone2",
                table: "UnityApplicantAgent");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "UnityApplicantAgent");
        }
    }
}