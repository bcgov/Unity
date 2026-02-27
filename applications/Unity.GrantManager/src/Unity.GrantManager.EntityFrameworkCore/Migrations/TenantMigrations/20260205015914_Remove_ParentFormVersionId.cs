using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class Remove_ParentFormVersionId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop FK constraint first
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationForms_ApplicationFormVersion_ParentFormVersionId",
                table: "ApplicationForms");

            // Drop index
            migrationBuilder.DropIndex(
                name: "IX_ApplicationForms_ParentFormVersionId",
                table: "ApplicationForms");

            // Drop column
            migrationBuilder.DropColumn(
                name: "ParentFormVersionId",
                table: "ApplicationForms");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Add column back
            migrationBuilder.AddColumn<Guid>(
                name: "ParentFormVersionId",
                table: "ApplicationForms",
                type: "uuid",
                nullable: true);

            // Recreate index
            migrationBuilder.CreateIndex(
                name: "IX_ApplicationForms_ParentFormVersionId",
                table: "ApplicationForms",
                column: "ParentFormVersionId");

            // Recreate FK constraint
            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationForms_ApplicationFormVersion_ParentFormVersionId",
                table: "ApplicationForms",
                column: "ParentFormVersionId",
                principalTable: "ApplicationFormVersion",
                principalColumn: "Id");
        }
    }
}
