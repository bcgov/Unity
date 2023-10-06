using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations
{
    /// <inheritdoc />
    public partial class AddAttachedByColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AttachedBy",
                table: "UnityApplicationAttachment",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AttachedBy",
                table: "UnityAdjudicationAttachment",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttachedBy",
                table: "UnityApplicationAttachment");

            migrationBuilder.DropColumn(
                name: "AttachedBy",
                table: "UnityAdjudicationAttachment");
        }
    }
}
