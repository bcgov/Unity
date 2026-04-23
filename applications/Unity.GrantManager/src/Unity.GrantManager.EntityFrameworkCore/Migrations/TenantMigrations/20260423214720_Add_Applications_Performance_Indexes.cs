using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class Add_Applications_Performance_Indexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ApplicationTags_TenantId_ApplicationId",
                table: "ApplicationTags",
                columns: new[] { "TenantId", "ApplicationId" });

            migrationBuilder.CreateIndex(
                name: "IX_Applications_ReferenceNo",
                table: "Applications",
                column: "ReferenceNo");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_TenantId_SubmissionDate",
                table: "Applications",
                columns: new[] { "TenantId", "SubmissionDate" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationLinks_TenantId_ApplicationId",
                table: "ApplicationLinks",
                columns: new[] { "TenantId", "ApplicationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationAssignments_TenantId_ApplicationId",
                table: "ApplicationAssignments",
                columns: new[] { "TenantId", "ApplicationId" });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicantAgents_TenantId_ApplicationId",
                table: "ApplicantAgents",
                columns: new[] { "TenantId", "ApplicationId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ApplicationTags_TenantId_ApplicationId",
                table: "ApplicationTags");

            migrationBuilder.DropIndex(
                name: "IX_Applications_ReferenceNo",
                table: "Applications");

            migrationBuilder.DropIndex(
                name: "IX_Applications_TenantId_SubmissionDate",
                table: "Applications");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationLinks_TenantId_ApplicationId",
                table: "ApplicationLinks");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationAssignments_TenantId_ApplicationId",
                table: "ApplicationAssignments");

            migrationBuilder.DropIndex(
                name: "IX_ApplicantAgents_TenantId_ApplicationId",
                table: "ApplicantAgents");
        }
    }
}
