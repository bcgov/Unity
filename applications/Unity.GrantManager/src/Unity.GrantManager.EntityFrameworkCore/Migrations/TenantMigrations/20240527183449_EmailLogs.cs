using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AddEmailLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Notifications");

            migrationBuilder.CreateTable(
                name: "EmailLogs",
                schema: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApplicationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ApplicantId = table.Column<Guid>(type: "uuid", nullable: true),
                    FromAddress = table.Column<string>(type: "text", nullable: true),
                    ToAddress = table.Column<string>(type: "text", nullable: false),
                    CC = table.Column<string>(type: "text", nullable: true),
                    BCC = table.Column<string>(type: "text", nullable: true),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    BodyType = table.Column<string>(type: "text", nullable: true),
                    Priority = table.Column<string>(type: "text", nullable: true),
                    SendOnDateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    SentDateTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Tag = table.Column<string>(type: "text", nullable: true),
                    RetryAttempts = table.Column<int>(type: "integer", nullable: true),
                    ChesMsgId = table.Column<string>(type: "text", nullable: true),
                    ChesTxnId = table.Column<string>(type: "text", nullable: true),
                    ChesResponse = table.Column<string>(type: "text", nullable: true),
                    ChesStatus = table.Column<string>(type: "text", nullable: true),
                    ChseHttpStatusCode = table.Column<string>(type: "text", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChesEmailLogs", x => x.Id);
                });

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "EmailLogs", schema: "Notifications");
        }
    }

}


