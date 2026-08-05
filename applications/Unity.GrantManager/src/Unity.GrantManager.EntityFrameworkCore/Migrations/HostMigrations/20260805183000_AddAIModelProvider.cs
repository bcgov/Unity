using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.HostMigrations;

[Migration("20260805183000_AddAIModelProvider")]
public partial class AddAIModelProvider : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Provider",
            schema: "AI",
            table: "AIModels",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false,
            defaultValue: "OpenAI");

        migrationBuilder.Sql(
            """
            UPDATE "AI"."AIModels"
            SET "Name" = CASE "Name"
                WHEN 'Gpt4oMini' THEN 'gpt-4o-mini'
                WHEN 'Gpt5Mini' THEN 'gpt-5-mini'
                WHEN 'Gpt5Nano' THEN 'gpt-5-nano'
                ELSE "Name"
            END
            WHERE "Name" IN ('Gpt4oMini', 'Gpt5Mini', 'Gpt5Nano');
            """);

        migrationBuilder.AlterColumn<string>(
            name: "Provider",
            schema: "AI",
            table: "AIModels",
            type: "character varying(100)",
            maxLength: 100,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(100)",
            oldMaxLength: 100,
            oldDefaultValue: "OpenAI");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE "AI"."AIModels"
            SET "Name" = CASE "Name"
                WHEN 'gpt-4o-mini' THEN 'Gpt4oMini'
                WHEN 'gpt-5-mini' THEN 'Gpt5Mini'
                WHEN 'gpt-5-nano' THEN 'Gpt5Nano'
                ELSE "Name"
            END
            WHERE "Name" IN ('gpt-4o-mini', 'gpt-5-mini', 'gpt-5-nano');
            """);

        migrationBuilder.DropColumn(
            name: "Provider",
            schema: "AI",
            table: "AIModels");
    }
}
