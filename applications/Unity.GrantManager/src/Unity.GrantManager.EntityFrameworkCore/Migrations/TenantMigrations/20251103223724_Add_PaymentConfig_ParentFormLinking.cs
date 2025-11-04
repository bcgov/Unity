using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class Add_PaymentConfig_ParentFormLinking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FormHierarchy",
                table: "ApplicationForms",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentFormId",
                table: "ApplicationForms",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentFormVersionId",
                table: "ApplicationForms",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationForms_ParentFormId",
                table: "ApplicationForms",
                column: "ParentFormId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationForms_ParentFormVersionId",
                table: "ApplicationForms",
                column: "ParentFormVersionId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationForms_ApplicationFormVersion_ParentFormVersionId",
                table: "ApplicationForms",
                column: "ParentFormVersionId",
                principalTable: "ApplicationFormVersion",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicationForms_ApplicationForms_ParentFormId",
                table: "ApplicationForms",
                column: "ParentFormId",
                principalTable: "ApplicationForms",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationForms_ApplicationFormVersion_ParentFormVersionId",
                table: "ApplicationForms");

            migrationBuilder.DropForeignKey(
                name: "FK_ApplicationForms_ApplicationForms_ParentFormId",
                table: "ApplicationForms");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationForms_ParentFormId",
                table: "ApplicationForms");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationForms_ParentFormVersionId",
                table: "ApplicationForms");

            migrationBuilder.DropColumn(
                name: "FormHierarchy",
                table: "ApplicationForms");

            migrationBuilder.DropColumn(
                name: "ParentFormId",
                table: "ApplicationForms");

            migrationBuilder.DropColumn(
                name: "ParentFormVersionId",
                table: "ApplicationForms");
        }
    }
}
