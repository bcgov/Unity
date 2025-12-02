using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class ApplicantElectoralDistrictUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApplicantElectoralDistrict",
                table: "Applications",
                type: "text",
                nullable: true);

            // Migrate data from old ElectoralDistrict column in Applicants to new ApplicantElectoralDistrict column in Applications
            migrationBuilder.Sql(@"
                UPDATE ""Applications"" a
                SET ""ApplicantElectoralDistrict"" = ap.""ElectoralDistrict""
                FROM ""Applicants"" ap
                WHERE a.""ApplicantId"" = ap.""Id"" AND ap.""ElectoralDistrict"" IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApplicantElectoralDistrict",
                table: "Applications");
        }
    }
}
