using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations;

public partial class AddGenerationReviews : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "AI");

        migrationBuilder.CreateTable(
            name: "GenerationReviews",
            schema: "AI",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Operation = table.Column<string>(type: "text", nullable: false),
                ContextId = table.Column<Guid>(type: "uuid", nullable: false),
                Sequence = table.Column<int>(type: "integer", nullable: false),
                Status = table.Column<string>(type: "text", nullable: false),
                ReviewData = table.Column<string>(type: "jsonb", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                ExtraProperties = table.Column<string>(type: "text", nullable: false),
                ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GenerationReviews", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_GenerationReviews_Operation_ContextId_Sequence",
            schema: "AI",
            table: "GenerationReviews",
            columns: new[] { "Operation", "ContextId", "Sequence" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "GenerationReviews",
            schema: "AI");
    }
}
