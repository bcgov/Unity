using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations
{
    /// <inheritdoc />
    public partial class ApplicationForm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<int>(
                name: "Version",
                table: "UnityApplicationForm",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AvailableChefsFields",
                table: "UnityApplicationForm",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubmissionHeaderMapping",
                table: "UnityApplicationForm",
                type: "text",
                nullable: true);
                
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
           migrationBuilder.DropColumn(
                name: "Version",
                table: "UnityApplicationForm");

            migrationBuilder.DropColumn(
                name: "AvailableChefsFields",
                table: "UnityApplicationForm");
            
            migrationBuilder.DropColumn(
                name: "SubmissionHeaderMapping",
                table: "UnityApplicationForm");
        }
    }
}
