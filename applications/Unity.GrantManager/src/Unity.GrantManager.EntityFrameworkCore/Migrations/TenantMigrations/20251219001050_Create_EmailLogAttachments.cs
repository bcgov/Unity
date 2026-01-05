using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class Create_EmailLogAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmailLogAttachments",
                schema: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailLogId = table.Column<Guid>(type: "uuid", nullable: false),
                    S3ObjectKey = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    ContentType = table.Column<string>(type: "text", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Time = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExtraProperties = table.Column<string>(type: "text", nullable: false),
                    ConcurrencyStamp = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModificationTime = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    LastModifierId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailLogAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmailLogAttachments_EmailLogs_EmailLogId",
                        column: x => x.EmailLogId,
                        principalSchema: "Notifications",
                        principalTable: "EmailLogs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogAttachments_EmailLogId",
                schema: "Notifications",
                table: "EmailLogAttachments",
                column: "EmailLogId");

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogAttachments_S3ObjectKey",
                schema: "Notifications",
                table: "EmailLogAttachments",
                column: "S3ObjectKey");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailLogAttachments",
                schema: "Notifications");
        }
    }
}
