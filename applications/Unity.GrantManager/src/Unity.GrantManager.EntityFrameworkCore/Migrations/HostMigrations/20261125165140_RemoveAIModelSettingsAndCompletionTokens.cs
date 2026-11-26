using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.HostMigrations
{
    /// <inheritdoc />
    public partial class RemoveAIModelSettingsAndCompletionTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletionTokens",
                schema: "AI",
                table: "AIOperations");

            migrationBuilder.DropColumn(
                name: "SettingsJson",
                schema: "AI",
                table: "AIModels");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CompletionTokens",
                schema: "AI",
                table: "AIOperations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SettingsJson",
                schema: "AI",
                table: "AIModels",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");
        }
    }
}
