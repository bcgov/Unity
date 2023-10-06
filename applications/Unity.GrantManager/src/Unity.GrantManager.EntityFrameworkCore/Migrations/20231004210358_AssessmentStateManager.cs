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
            migrationBuilder.DropColumn(
                name: "AssessorName",
                table: "UnityAssessment");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "UnityAssessment");

            migrationBuilder.AddColumn<Guid>(
                name: "AssessorId",
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
                name: "IX_UnityAssessment_AssessorId",
                table: "UnityAssessment",
                column: "AssessorId");

            migrationBuilder.AddForeignKey(
                name: "FK_UnityAssessment_AbpUsers_AssessorId",
                table: "UnityAssessment",
                column: "AssessorId",
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
                name: "FK_UnityAssessment_AbpUsers_AssessorId",
                table: "UnityAssessment");

            migrationBuilder.DropForeignKey(
                name: "FK_UnityAssessment_UnityApplication_ApplicationId",
                table: "UnityAssessment");

            migrationBuilder.DropIndex(
                name: "IX_UnityAssessment_ApplicationId",
                table: "UnityAssessment");

            migrationBuilder.DropIndex(
                name: "IX_UnityAssessment_AssessorId",
                table: "UnityAssessment");

            migrationBuilder.DropColumn(
                name: "AssessorId",
                table: "UnityAssessment");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "UnityAssessment");

            migrationBuilder.AddColumn<string>(
                name: "AssessorName",
                table: "UnityAssessment",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "UnityAssessment",
                type: "timestamp without time zone",
                nullable: true);
        }
    }
}
