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
            migrationBuilder.Sql(
                """
                ALTER TABLE "AI"."AIPrompts"
                ADD COLUMN IF NOT EXISTS "VersionNumber" integer NOT NULL DEFAULT 1;
                """);

            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "AI"."IX_AIPrompts_Name";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_AIPrompts_TenantId_Name_VersionNumber",
                schema: "AI",
                table: "AIPrompts",
                columns: new[] { "TenantId", "Name", "VersionNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AIPrompts_TenantId_Name_VersionNumber",
                schema: "AI",
                table: "AIPrompts");
        }
    }
}
