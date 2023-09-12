using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations
{
    /// <inheritdoc />
    public partial class AssessmentStateManager : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AssignedUserId",
                table: "UnityAssessment",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "UnityAssessment",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_UnityAssessment_ApplicationId",
                table: "UnityAssessment",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_UnityAssessment_AssignedUserId",
                table: "UnityAssessment",
                column: "AssignedUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_UnityAssessment_AbpUsers_AssignedUserId",
                table: "UnityAssessment",
                column: "AssignedUserId",
                principalTable: "AbpUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UnityAssessment_UnityApplication_ApplicationId",
                table: "UnityAssessment",
                column: "ApplicationId",
                principalTable: "UnityApplication",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UnityAssessment_AbpUsers_AssignedUserId",
                table: "UnityAssessment");

            migrationBuilder.DropForeignKey(
                name: "FK_UnityAssessment_UnityApplication_ApplicationId",
                table: "UnityAssessment");

            migrationBuilder.DropIndex(
                name: "IX_UnityAssessment_ApplicationId",
                table: "UnityAssessment");

            migrationBuilder.DropIndex(
                name: "IX_UnityAssessment_AssignedUserId",
                table: "UnityAssessment");

            migrationBuilder.DropColumn(
                name: "AssignedUserId",
                table: "UnityAssessment");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "UnityAssessment");
        }
    }
}
