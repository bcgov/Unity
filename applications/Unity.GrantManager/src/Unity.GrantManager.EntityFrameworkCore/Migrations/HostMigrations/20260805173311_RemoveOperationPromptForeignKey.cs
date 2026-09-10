using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.HostMigrations
{
    /// <inheritdoc />
    public partial class RemoveOperationPromptForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE "AI"."AIOperations"
                DROP CONSTRAINT IF EXISTS "FK_AIOperations_AIPrompts_AIPromptId";
                """);

            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "AI"."IX_AIOperations_AIPromptId";
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE "AI"."AIOperations"
                DROP COLUMN IF EXISTS "AIPromptId";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException(
                "RemoveOperationPromptForeignKey cannot be rolled back because prompt selection is now resolved by tenant/global prompt family.");
        }
    }
}
