using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.HostMigrations
{
    /// <inheritdoc />
    public partial class AddExceptionLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
           
            migrationBuilder.CreateTable(
                name: "ExceptionLogs",
                schema: null,
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    TenantName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NotificationType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Channel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Severity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SourceReference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    OccurrenceCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    IsDeliveredRealtime = table.Column<bool>(type: "boolean", nullable: false),
                    DeliveryTarget = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ExceptionType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ExceptionMessage = table.Column<string>(type: "text", nullable: true),
                    StackExcerpt = table.Column<string>(type: "text", nullable: true),
                    SourceFile = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    SourceLine = table.Column<int>(type: "integer", nullable: true),
                    CommitSha = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Environment = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    BlameAuthor = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    BlameEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    BlameCommitSha = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    BlameCommitMessage = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    PullRequestUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    PullRequestNumber = table.Column<int>(type: "integer", nullable: true),
                    PullRequestTitle = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    TicketReference = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExceptionLogs", x => x.Id);
                });

            
            migrationBuilder.CreateIndex(
                name: "IX_ExceptionLogs_CorrelationId",
                table: "ExceptionLogs",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionLogs_NotificationType_CreationTime",
                table: "ExceptionLogs",
                columns: new[] { "NotificationType", "CreationTime" });

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionLogs_TenantId_CreationTime",
                table: "ExceptionLogs",
                columns: new[] { "TenantId", "CreationTime" });

            migrationBuilder.CreateIndex(
                name: "IX_ExceptionLogs_TicketReference",
                table: "ExceptionLogs",
                column: "TicketReference");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropTable(
                name: "ExceptionLogs");

        }
    }
}
