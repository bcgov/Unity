using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.HostMigrations
{
    /// <inheritdoc />
    public partial class CorrectStatusWorkflowRoleDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PermissionGrants is shared by the host and all tenants in the host database.
            // Keep these historical identifiers literal so later permission renames cannot change this migration.
            // Only the two retired Assessor role defaults are removed; direct user grants and other permissions remain.
            migrationBuilder.Sql("""
                DELETE FROM "PermissionGrants"
                WHERE "ProviderName" = 'R'
                  AND "ProviderKey" = 'assessor'
                  AND "Name" IN (
                      'GrantApplicationManagement.Reviews.StartInitial',
                      'GrantApplicationManagement.Reviews.CompleteInitial'
                  );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally leave permission assignments unchanged on rollback. Regranting both permissions
            // would create access for tenants that had already removed them before this migration.

        }
    }
}
