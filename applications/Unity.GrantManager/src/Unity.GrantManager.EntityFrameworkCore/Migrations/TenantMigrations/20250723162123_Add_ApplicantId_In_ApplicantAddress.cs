using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class Add_ApplicantId_In_ApplicantAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ApplicationId",
                table: "ApplicantAddresses",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantAddresses_ApplicationId",
                table: "ApplicantAddresses",
                column: "ApplicationId");

            migrationBuilder.AddForeignKey(
                name: "FK_ApplicantAddresses_Applications_ApplicationId",
                table: "ApplicantAddresses",
                column: "ApplicationId",
                principalTable: "Applications",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ApplicantAddresses_Applications_ApplicationId",
                table: "ApplicantAddresses");

            migrationBuilder.DropIndex(
                name: "IX_ApplicantAddresses_ApplicationId",
                table: "ApplicantAddresses");

            migrationBuilder.DropColumn(
                name: "ApplicationId",
                table: "ApplicantAddresses");
        }
    }
}
