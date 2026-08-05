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
            migrationBuilder.AddColumn<Guid>(
                name: "AIPromptId",
                schema: "AI",
                table: "AIOperations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_AIOperations_AIPromptId",
                schema: "AI",
                table: "AIOperations",
                column: "AIPromptId");

            migrationBuilder.AddForeignKey(
                name: "FK_AIOperations_AIPrompts_AIPromptId",
                schema: "AI",
                table: "AIOperations",
                column: "AIPromptId",
                principalSchema: "AI",
                principalTable: "AIPrompts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
