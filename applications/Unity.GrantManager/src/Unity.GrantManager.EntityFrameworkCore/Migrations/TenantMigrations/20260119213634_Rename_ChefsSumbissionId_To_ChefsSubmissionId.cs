using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class Rename_ChefsSumbissionId_To_ChefsSubmissionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ChefsSumbissionId",
                table: "ApplicationChefsFileAttachments",
                newName: "ChefsSubmissionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ChefsSubmissionId",
                table: "ApplicationChefsFileAttachments",
                newName: "ChefsSumbissionId");
        }
    }
}
