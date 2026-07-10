using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AddRecipientColumnsToEmailTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "RecipientCategory",
                schema: "Notifications",
                table: "EmailTemplates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecipientIdentifier",
                schema: "Notifications",
                table: "EmailTemplates",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OriginTemplateId",
                schema: "Notifications",
                table: "EmailLogAttachments",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RecipientCategory",
                schema: "Notifications",
                table: "EmailTemplates");

            migrationBuilder.DropColumn(
                name: "RecipientIdentifier",
                schema: "Notifications",
                table: "EmailTemplates");

            migrationBuilder.DropColumn(
                name: "OriginTemplateId",
                schema: "Notifications",
                table: "EmailLogAttachments");
        }
    }
}
