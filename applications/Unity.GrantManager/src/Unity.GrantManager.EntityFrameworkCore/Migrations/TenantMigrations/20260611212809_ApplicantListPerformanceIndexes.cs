using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class ApplicantListPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Applicants_OrgName",
                table: "Applicants",
                column: "OrgName");

            migrationBuilder.CreateIndex(
                name: "IX_Applicants_OrgNumber",
                table: "Applicants",
                column: "OrgNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Applicants_Status",
                table: "Applicants",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Applicants_SupplierId",
                table: "Applicants",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_Applicants_TenantId_IsDeleted_CreationTime",
                table: "Applicants",
                columns: new[] { "TenantId", "IsDeleted", "CreationTime" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Applicants_UnityApplicantId",
                table: "Applicants",
                column: "UnityApplicantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Applicants_OrgName",
                table: "Applicants");

            migrationBuilder.DropIndex(
                name: "IX_Applicants_OrgNumber",
                table: "Applicants");

            migrationBuilder.DropIndex(
                name: "IX_Applicants_Status",
                table: "Applicants");

            migrationBuilder.DropIndex(
                name: "IX_Applicants_SupplierId",
                table: "Applicants");

            migrationBuilder.DropIndex(
                name: "IX_Applicants_TenantId_IsDeleted_CreationTime",
                table: "Applicants");

            migrationBuilder.DropIndex(
                name: "IX_Applicants_UnityApplicantId",
                table: "Applicants");
        }
    }
}
