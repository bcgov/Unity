using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.HostMigrations;

public partial class AddAIRequestTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "AIRequests",
            schema: "AI",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                ApplicationId = table.Column<Guid>(type: "uuid", nullable: true),
                OperationType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                RequestKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                StartedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                CompletedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                FailureReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                DeleterId = table.Column<Guid>(type: "uuid", nullable: true),
                DeletionTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                ExtraProperties = table.Column<string>(type: "text", nullable: false),
                ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AIRequests", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AIRequests_TenantId_ApplicationId_OperationType_Status",
            schema: "AI",
            table: "AIRequests",
            columns: new[] { "TenantId", "ApplicationId", "OperationType", "Status" });

        migrationBuilder.CreateIndex(
            name: "IX_AIRequests_RequestKey",
            schema: "AI",
            table: "AIRequests",
            column: "RequestKey");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "AIRequests",
            schema: "AI");
    }
}
