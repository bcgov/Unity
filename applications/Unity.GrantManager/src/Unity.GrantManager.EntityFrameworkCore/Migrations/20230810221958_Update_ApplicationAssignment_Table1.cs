using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations
{
    /// <inheritdoc />
    public partial class UpdateApplicationAssignmentTable1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UnityApplicationUserAssignment_UnityTeam_TeamId",
                table: "UnityApplicationUserAssignment");

            migrationBuilder.AlterColumn<Guid>(
                name: "TeamId",
                table: "UnityApplicationUserAssignment",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_UnityApplicationUserAssignment_UnityTeam_TeamId",
                table: "UnityApplicationUserAssignment",
                column: "TeamId",
                principalTable: "UnityTeam",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UnityApplicationUserAssignment_UnityTeam_TeamId",
                table: "UnityApplicationUserAssignment");

            migrationBuilder.AlterColumn<Guid>(
                name: "TeamId",
                table: "UnityApplicationUserAssignment",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UnityApplicationUserAssignment_UnityTeam_TeamId",
                table: "UnityApplicationUserAssignment",
                column: "TeamId",
                principalTable: "UnityTeam",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
