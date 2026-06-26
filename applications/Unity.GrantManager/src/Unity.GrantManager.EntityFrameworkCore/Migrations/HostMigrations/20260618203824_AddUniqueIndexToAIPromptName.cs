using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.HostMigrations
{
    /// <inheritdoc />
    public partial class AddUniqueIndexToAIPromptName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AIPrompts_Name",
                schema: "AI",
                table: "AIPrompts",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AIPrompts_Name",
                schema: "AI",
                table: "AIPrompts");
        }
    }
}
