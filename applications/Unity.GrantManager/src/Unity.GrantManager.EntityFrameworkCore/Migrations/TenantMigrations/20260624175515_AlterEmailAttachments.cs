using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AlterEmailAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Make EmailLogId nullable
            migrationBuilder.AlterColumn<Guid>(
                name: "EmailLogId",
                schema: "Notifications",
                table: "EmailLogAttachments",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            // Add TemplateId
            migrationBuilder.AddColumn<Guid>(
                name: "TemplateId",
                schema: "Notifications",
                table: "EmailLogAttachments",
                type: "uuid",
                nullable: true);

            // Optional FK - adjust schema/table as needed
            migrationBuilder.AddForeignKey(
                name: "FK_EmailLogAttachments_EmailTemplates_TemplateId",
                schema: "Notifications",
                table: "EmailLogAttachments",
                column: "TemplateId",
                principalSchema: "Notifications",
                principalTable: "EmailTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.CreateIndex(
                name: "IX_EmailLogAttachments_TemplateId",
                schema: "Notifications",
                table: "EmailLogAttachments",
                column: "TemplateId");

            // ARC constraint:
            // Exactly one of EmailLogId or TemplateId must be populated.
            migrationBuilder.AddCheckConstraint(
                name: "CK_EmailLogAttachments_EmailLogId_XOR_TemplateId",
                schema: "Notifications",
                table: "EmailLogAttachments",
                sql: @"
                    (
                        (""EmailLogId"" IS NOT NULL AND ""TemplateId"" IS NULL)
                        OR
                        (""EmailLogId"" IS NULL AND ""TemplateId"" IS NOT NULL)
                    )");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_EmailLogAttachments_EmailLogId_XOR_TemplateId",
                schema: "Notifications",
                table: "EmailLogAttachments");

            migrationBuilder.DropForeignKey(
                name: "FK_EmailLogAttachments_EmailTemplates_TemplateId",
                schema: "Notifications",
                table: "EmailLogAttachments");

            migrationBuilder.DropIndex(
                name: "IX_EmailLogAttachments_TemplateId",
                schema: "Notifications",
                table: "EmailLogAttachments");

            migrationBuilder.DropColumn(
                name: "TemplateId",
                schema: "Notifications",
                table: "EmailLogAttachments");

            migrationBuilder.AlterColumn<Guid>(
                name: "EmailLogId",
                schema: "Notifications",
                table: "EmailLogAttachments",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

        }
    }
}
