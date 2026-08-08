using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Unity.GrantManager.EntityFrameworkCore;

#nullable disable

namespace Unity.GrantManager.Migrations.HostMigrations;

[DbContext(typeof(GrantManagerDbContext))]
[Migration("20260805185000_RebuildAIModels")]
public partial class RebuildAIModels : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE "AI"."AIOperations"
                DROP CONSTRAINT IF EXISTS "FK_AIOperations_AIModels_AIModelId";

            DROP INDEX IF EXISTS "AI"."IX_AIOperations_AIModelId";
            DROP INDEX IF EXISTS "AI"."IX_AIModels_Name";

            ALTER TABLE "AI"."AIModels"
                RENAME TO "AIModels_Legacy";

            ALTER TABLE "AI"."AIModels_Legacy"
                RENAME CONSTRAINT "PK_AIModels" TO "PK_AIModels_Legacy";

            CREATE TABLE "AI"."AIModels"
            (
                "Id" uuid NOT NULL,
                "Name" character varying(200) NOT NULL,
                "Provider" character varying(100) NOT NULL,
                "IsActive" boolean NOT NULL,
                "SettingsJson" jsonb NOT NULL,
                "ExtraProperties" text NOT NULL,
                "ConcurrencyStamp" character varying(40) NOT NULL,
                "CreationTime" timestamp without time zone NOT NULL,
                "CreatorId" uuid NULL,
                "LastModificationTime" timestamp without time zone NULL,
                "LastModifierId" uuid NULL,
                CONSTRAINT "PK_AIModels" PRIMARY KEY ("Id")
            );

            INSERT INTO "AI"."AIModels"
            (
                "Id",
                "Name",
                "Provider",
                "IsActive",
                "SettingsJson",
                "ExtraProperties",
                "ConcurrencyStamp",
                "CreationTime",
                "CreatorId",
                "LastModificationTime",
                "LastModifierId"
            )
            SELECT
                "Id",
                CASE "Name"
                    WHEN 'Gpt4oMini' THEN 'gpt-4o-mini'
                    WHEN 'Gpt5Mini' THEN 'gpt-5-mini'
                    WHEN 'Gpt5Nano' THEN 'gpt-5-nano'
                    ELSE "Name"
                END,
                'OpenAI',
                "IsActive",
                "SettingsJson",
                "ExtraProperties",
                "ConcurrencyStamp",
                "CreationTime",
                "CreatorId",
                "LastModificationTime",
                "LastModifierId"
            FROM "AI"."AIModels_Legacy";

            DROP TABLE "AI"."AIModels_Legacy";

            CREATE UNIQUE INDEX "IX_AIModels_Name"
                ON "AI"."AIModels" ("Name");

            CREATE INDEX "IX_AIOperations_AIModelId"
                ON "AI"."AIOperations" ("AIModelId");

            ALTER TABLE "AI"."AIOperations"
                ADD CONSTRAINT "FK_AIOperations_AIModels_AIModelId"
                FOREIGN KEY ("AIModelId")
                REFERENCES "AI"."AIModels" ("Id")
                ON DELETE RESTRICT;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            ALTER TABLE "AI"."AIOperations"
                DROP CONSTRAINT IF EXISTS "FK_AIOperations_AIModels_AIModelId";

            DROP INDEX IF EXISTS "AI"."IX_AIOperations_AIModelId";
            DROP INDEX IF EXISTS "AI"."IX_AIModels_Name";

            ALTER TABLE "AI"."AIModels"
                RENAME TO "AIModels_Current";

            ALTER TABLE "AI"."AIModels_Current"
                RENAME CONSTRAINT "PK_AIModels" TO "PK_AIModels_Current";

            CREATE TABLE "AI"."AIModels"
            (
                "Id" uuid NOT NULL,
                "Name" character varying(200) NOT NULL,
                "IsActive" boolean NOT NULL,
                "SettingsJson" jsonb NOT NULL,
                "ExtraProperties" text NOT NULL,
                "ConcurrencyStamp" character varying(40) NOT NULL,
                "CreationTime" timestamp without time zone NOT NULL,
                "CreatorId" uuid NULL,
                "LastModificationTime" timestamp without time zone NULL,
                "LastModifierId" uuid NULL,
                CONSTRAINT "PK_AIModels" PRIMARY KEY ("Id")
            );

            INSERT INTO "AI"."AIModels"
            (
                "Id",
                "Name",
                "IsActive",
                "SettingsJson",
                "ExtraProperties",
                "ConcurrencyStamp",
                "CreationTime",
                "CreatorId",
                "LastModificationTime",
                "LastModifierId"
            )
            SELECT
                "Id",
                CASE "Name"
                    WHEN 'gpt-4o-mini' THEN 'Gpt4oMini'
                    WHEN 'gpt-5-mini' THEN 'Gpt5Mini'
                    WHEN 'gpt-5-nano' THEN 'Gpt5Nano'
                    ELSE "Name"
                END,
                "IsActive",
                "SettingsJson",
                "ExtraProperties",
                "ConcurrencyStamp",
                "CreationTime",
                "CreatorId",
                "LastModificationTime",
                "LastModifierId"
            FROM "AI"."AIModels_Current";

            DROP TABLE "AI"."AIModels_Current";

            CREATE UNIQUE INDEX "IX_AIModels_Name"
                ON "AI"."AIModels" ("Name");

            CREATE INDEX "IX_AIOperations_AIModelId"
                ON "AI"."AIOperations" ("AIModelId");

            ALTER TABLE "AI"."AIOperations"
                ADD CONSTRAINT "FK_AIOperations_AIModels_AIModelId"
                FOREIGN KEY ("AIModelId")
                REFERENCES "AI"."AIModels" ("Id")
                ON DELETE RESTRICT;
            """);
    }
}
