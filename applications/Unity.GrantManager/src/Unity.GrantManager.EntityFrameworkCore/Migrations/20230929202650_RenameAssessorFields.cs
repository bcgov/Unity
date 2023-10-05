using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations
{
    /// <inheritdoc />
    public partial class RenameAssessorFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AdjudicatorName",
                table: "UnityAssessment",
                newName: "AssessorName");

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "UnityApplicationAttachment",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AssessorName",
                table: "UnityAssessment",
                newName: "AdjudicatorName");

            migrationBuilder.AlterColumn<string>(
                name: "FileName",
                table: "UnityApplicationAttachment",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");
        }
    }
}
