using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class Add_Supporting_Table_TenantId_Indexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Persons_TenantId",
                table: "Persons",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationForms_TenantId_IsDeleted",
                table: "ApplicationForms",
                columns: new[] { "TenantId", "IsDeleted" },
                filter: "\"IsDeleted\" = false");

            migrationBuilder.CreateIndex(
                name: "IX_Applicants_TenantId",
                table: "Applicants",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Persons_TenantId",
                table: "Persons");

            migrationBuilder.DropIndex(
                name: "IX_ApplicationForms_TenantId_IsDeleted",
                table: "ApplicationForms");

            migrationBuilder.DropIndex(
                name: "IX_Applicants_TenantId",
                table: "Applicants");
        }
    }
}
