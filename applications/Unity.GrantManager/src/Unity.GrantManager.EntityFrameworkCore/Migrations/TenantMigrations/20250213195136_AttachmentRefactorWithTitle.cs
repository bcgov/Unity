using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AttachmentRefactorWithTitle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "ApplicationChefsFileAttachments",
                newName: "FileName");

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "AssessmentAttachments",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "ApplicationChefsFileAttachments",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "ApplicationAttachments",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "AssessmentAttachments");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "ApplicationChefsFileAttachments");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "ApplicationAttachments");

            migrationBuilder.RenameColumn(
                name: "FileName",
                table: "ApplicationChefsFileAttachments",
                newName: "Name");
        }
    }
}
