using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class Update_Flex_Tables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ScoresheetId",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "AssessmentId",
                schema: "Flex",
                table: "Answers");

            migrationBuilder.DropColumn(
                name: "CorrelationProvider",
                schema: "Flex",
                table: "Answers");

            migrationBuilder.RenameColumn(
                name: "CorrelationId",
                schema: "Flex",
                table: "Answers",
                newName: "ScoresheetInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_Answers_ScoresheetInstanceId",
                schema: "Flex",
                table: "Answers",
                column: "ScoresheetInstanceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Answers_ScoresheetInstances_ScoresheetInstanceId",
                schema: "Flex",
                table: "Answers",
                column: "ScoresheetInstanceId",
                principalSchema: "Flex",
                principalTable: "ScoresheetInstances",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Answers_ScoresheetInstances_ScoresheetInstanceId",
                schema: "Flex",
                table: "Answers");

            migrationBuilder.DropIndex(
                name: "IX_Answers_ScoresheetInstanceId",
                schema: "Flex",
                table: "Answers");

            migrationBuilder.RenameColumn(
                name: "ScoresheetInstanceId",
                schema: "Flex",
                table: "Answers",
                newName: "CorrelationId");

            migrationBuilder.AddColumn<Guid>(
                name: "ScoresheetId",
                table: "Assessments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AssessmentId",
                schema: "Flex",
                table: "Answers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "CorrelationProvider",
                schema: "Flex",
                table: "Answers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
