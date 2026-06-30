using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.HostMigrations
{
    /// <inheritdoc />
    public partial class RefreshAiSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS "AI"."AIPromptVersions";
                DROP TABLE IF EXISTS "AI"."AIPrompts";
                """);

            migrationBuilder.Sql(
                """
                CREATE TABLE "AI"."AIModels" (
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
                    "DeleterId" uuid NULL,
                    "DeletionTime" timestamp without time zone NULL,
                    "IsDeleted" boolean NOT NULL DEFAULT FALSE,
                    PRIMARY KEY ("Id")
                );
                """);

            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_AIModels_Name"
                ON "AI"."AIModels" ("Name");
                """);

            migrationBuilder.Sql(
                """
                CREATE TABLE "AI"."AIPrompts" (
                    "Id" uuid NOT NULL,
                    "TenantId" uuid NULL,
                    "Name" character varying(200) NOT NULL,
                    "VersionNumber" integer NOT NULL,
                    "SystemPrompt" text NOT NULL,
                    "UserPrompt" text NOT NULL,
                    "MetadataJson" jsonb NULL,
                    "IsActive" boolean NOT NULL,
                    "ExtraProperties" text NOT NULL,
                    "ConcurrencyStamp" character varying(40) NOT NULL,
                    "CreationTime" timestamp without time zone NOT NULL,
                    "CreatorId" uuid NULL,
                    "LastModificationTime" timestamp without time zone NULL,
                    "LastModifierId" uuid NULL,
                    "DeleterId" uuid NULL,
                    "DeletionTime" timestamp without time zone NULL,
                    "IsDeleted" boolean NOT NULL DEFAULT FALSE,
                    PRIMARY KEY ("Id")
                );
                """);

            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX "IX_AIPrompts_Name_VersionNumber"
                ON "AI"."AIPrompts" ("TenantId", "Name", "VersionNumber");
                """);

            migrationBuilder.Sql(
                """
                CREATE TABLE "AI"."AIOperations" (
                    "Id" uuid NOT NULL,
                    "Name" character varying(200) NOT NULL,
                    "AIModelId" uuid NOT NULL,
                    "AIPromptId" uuid NOT NULL,
                    "ExecutionMode" character varying(20) NOT NULL,
                    "CompletionTokens" integer NOT NULL,
                    "IsActive" boolean NOT NULL,
                    "ExtraProperties" text NOT NULL,
                    "ConcurrencyStamp" character varying(40) NOT NULL,
                    "CreationTime" timestamp without time zone NOT NULL,
                    "CreatorId" uuid NULL,
                    "LastModificationTime" timestamp without time zone NULL,
                    "LastModifierId" uuid NULL,
                    "DeleterId" uuid NULL,
                    "DeletionTime" timestamp without time zone NULL,
                    "IsDeleted" boolean NOT NULL DEFAULT FALSE,
                    PRIMARY KEY ("Id")
                );
                """);

            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_AIOperations_Name"
                ON "AI"."AIOperations" ("Name");
                """);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'FK_AIOperations_AIModels_AIModelId'
                    ) THEN
                        ALTER TABLE "AI"."AIOperations"
                        ADD CONSTRAINT "FK_AIOperations_AIModels_AIModelId"
                        FOREIGN KEY ("AIModelId") REFERENCES "AI"."AIModels" ("Id") ON DELETE RESTRICT;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'FK_AIOperations_AIPrompts_AIPromptId'
                    ) THEN
                        ALTER TABLE "AI"."AIOperations"
                        ADD CONSTRAINT "FK_AIOperations_AIPrompts_AIPromptId"
                        FOREIGN KEY ("AIPromptId") REFERENCES "AI"."AIPrompts" ("Id") ON DELETE RESTRICT;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS "AI"."AIPromptVersions";
                """);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM information_schema.columns
                        WHERE table_schema = 'AI'
                          AND table_name = 'AIRequests'
                          AND column_name = 'OperationId'
                    ) THEN
                        ALTER TABLE "AI"."AIRequests"
                        ADD COLUMN "OperationId" uuid NULL;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_AIRequests_RequestKey"
                ON "AI"."AIRequests" ("RequestKey");
                """);

            migrationBuilder.Sql(
                """
                CREATE INDEX IF NOT EXISTS "IX_AIRequests_OperationId"
                ON "AI"."AIRequests" ("OperationId");
                """);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1
                        FROM pg_constraint
                        WHERE conname = 'FK_AIRequests_AIOperations_OperationId'
                    ) THEN
                        ALTER TABLE "AI"."AIRequests"
                        ADD CONSTRAINT "FK_AIRequests_AIOperations_OperationId"
                        FOREIGN KEY ("OperationId") REFERENCES "AI"."AIOperations" ("Id") ON DELETE RESTRICT;
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql(
                """
                CREATE INDEX "IX_AIRequests_TenantId_ApplicationId_OperationId_Status"
                ON "AI"."AIRequests" ("TenantId", "ApplicationId", "OperationId", "Status");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE IF EXISTS "AI"."AIRequests";
                DROP TABLE IF EXISTS "AI"."AIPromptVersions";
                DROP TABLE IF EXISTS "AI"."AIPrompts";
                """);
        }
    }
}
