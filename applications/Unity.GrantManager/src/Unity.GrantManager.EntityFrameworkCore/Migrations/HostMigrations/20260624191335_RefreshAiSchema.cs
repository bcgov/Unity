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
                DROP INDEX IF EXISTS "AI"."IX_AIPrompts_Name";
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE "AI"."AIRequests"
                ADD COLUMN IF NOT EXISTS "OperationId" uuid;
                """);

            migrationBuilder.Sql(
                """
                UPDATE "AI"."AIRequests" r
                SET "OperationId" = o."Id"
                FROM "AI"."AIOperations" o
                WHERE r."OperationId" IS NULL
                  AND (
                      (r."OperationType" = 'application-analysis' AND o."Name" = 'ApplicationAnalysis')
                      OR (r."OperationType" = 'attachment-summary' AND o."Name" = 'AttachmentSummary')
                      OR (r."OperationType" = 'application-scoring' AND o."Name" = 'ApplicationScoring')
                      OR (r."OperationType" = 'pipeline' AND o."Name" = 'Default')
                  );
                """);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM "AI"."AIRequests"
                        WHERE "OperationId" IS NULL
                    ) THEN
                        RAISE EXCEPTION 'AIRequests contains rows that cannot be mapped to OperationId.';
                    END IF;
                END $$;
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE "AI"."AIRequests"
                ALTER COLUMN "OperationId" SET NOT NULL;
                """);

            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "AI"."IX_AIRequests_TenantId_ApplicationId_OperationType_Status";
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE "AI"."AIRequests"
                DROP COLUMN IF EXISTS "OperationType";
                """);

            migrationBuilder.CreateIndex(
                name: "IX_AIRequests_TenantId_ApplicationId_OperationId_Status",
                schema: "AI",
                table: "AIRequests",
                columns: new[] { "TenantId", "ApplicationId", "OperationId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AIRequests_OperationId",
                schema: "AI",
                table: "AIRequests",
                column: "OperationId");

            migrationBuilder.Sql(
                """
                ALTER TABLE "AI"."AIRequests"
                ADD CONSTRAINT "FK_AIRequests_AIOperations_OperationId"
                FOREIGN KEY ("OperationId") REFERENCES "AI"."AIOperations" ("Id") ON DELETE RESTRICT;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP INDEX IF EXISTS "AI"."IX_AIRequests_TenantId_ApplicationId_OperationType_Status";
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE "AI"."AIRequests"
                DROP CONSTRAINT IF EXISTS "FK_AIRequests_AIOperations_OperationId";
                """);

            migrationBuilder.DropIndex(
                name: "IX_AIRequests_OperationId",
                schema: "AI",
                table: "AIRequests");

            migrationBuilder.DropIndex(
                name: "IX_AIRequests_TenantId_ApplicationId_OperationId_Status",
                schema: "AI",
                table: "AIRequests");

            migrationBuilder.Sql(
                """
                ALTER TABLE "AI"."AIRequests"
                ADD COLUMN IF NOT EXISTS "OperationType" character varying(64) NOT NULL DEFAULT '';
                """);

            migrationBuilder.Sql(
                """
                UPDATE "AI"."AIRequests"
                SET "OperationType" = CASE
                    WHEN "OperationId" IS NOT NULL THEN
                        COALESCE(
                            (SELECT CASE o."Name"
                                WHEN 'ApplicationAnalysis' THEN 'application-analysis'
                                WHEN 'AttachmentSummary' THEN 'attachment-summary'
                                WHEN 'ApplicationScoring' THEN 'application-scoring'
                                WHEN 'Default' THEN 'pipeline'
                            END
                            FROM "AI"."AIOperations" o
                            WHERE o."Id" = "OperationId"),
                            'application-analysis'
                        )
                    ELSE 'application-analysis'
                END;
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE "AI"."AIRequests"
                ALTER COLUMN "OperationType" DROP DEFAULT;
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE "AI"."AIRequests"
                ALTER COLUMN "OperationType" SET NOT NULL;
                """);

            migrationBuilder.Sql(
                """
                CREATE UNIQUE INDEX IF NOT EXISTS "IX_AIRequests_TenantId_ApplicationId_OperationType_Status"
                ON "AI"."AIRequests" ("TenantId", "ApplicationId", "OperationType", "Status");
                """);
        }
    }
}
