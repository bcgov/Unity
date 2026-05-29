using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AddApplicationDefaultSiteIdAndCopyFromApplicant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add the new column on Applications (nullable to accept existing rows).
            migrationBuilder.AddColumn<Guid>(
                name: "DefaultSiteId",
                table: "Applications",
                type: "uuid",
                nullable: true);

            // 2. Copy Applicant.SiteId to every Application via the ApplicantId FK.
            migrationBuilder.Sql(@"
                UPDATE public.""Applications"" app
                SET ""DefaultSiteId"" = ap.""SiteId""
                FROM public.""Applicants"" ap
                WHERE app.""ApplicantId"" = ap.""Id""
                  AND ap.""SiteId"" IS NOT NULL;
            ");

            // 3. Safety: null out any DefaultSiteId that does not resolve to a real
            //    Site row. Protects the FK creation below from failing on orphaned
            //    references (shouldn't exist in practice, but defend anyway).
            migrationBuilder.Sql(@"
                UPDATE public.""Applications"" app
                SET ""DefaultSiteId"" = NULL
                WHERE ""DefaultSiteId"" IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1 FROM ""Payments"".""Sites"" s
                      WHERE s.""Id"" = app.""DefaultSiteId""
                  );
            ");

            // 4. Supporting index on the FK column.
            migrationBuilder.CreateIndex(
                name: "IX_Applications_DefaultSiteId",
                table: "Applications",
                column: "DefaultSiteId");

            // 5. Cross-schema FK: Applications.DefaultSiteId -> Payments.Sites.Id.
            //    ON DELETE NO ACTION matches EF's default and keeps the existing
            //    in-app "site in use" check as defence-in-depth.
            migrationBuilder.AddForeignKey(
                name: "FK_Applications_Sites_DefaultSiteId",
                table: "Applications",
                column: "DefaultSiteId",
                principalSchema: "Payments",
                principalTable: "Sites",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            // NOTE: Applicants."SiteId" is intentionally LEFT IN PLACE.
            //       It is no longer mapped in the EF model (see Applicant.cs change)
            //       so no code can read/write it, but the data stays untouched
            //       to keep rollback of this PR trivial. A follow-up cleanup
            //       migration (handled separately) will drop the column once
            //       stability is confirmed in PROD.
            //       The auto-generated `DropColumn("SiteId", "Applicants")` has
            //       been deleted by hand.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop FK, index, then column — reverse order of Up(). No need to
            // re-populate Applicants.SiteId — it was never dropped in Up(), so it
            // still holds the original values.
            migrationBuilder.DropForeignKey(
                name: "FK_Applications_Sites_DefaultSiteId",
                table: "Applications");

            migrationBuilder.DropIndex(
                name: "IX_Applications_DefaultSiteId",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "DefaultSiteId",
                table: "Applications");

            // NOTE: The auto-generated `AddColumn("SiteId", "Applicants", ...)`
            //       has been deleted by hand (the DB column was never dropped).
        }
    }
}
