using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.HostMigrations
{
    /// <inheritdoc />
    public partial class ReplaceGrantManagerDefaultPermission : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // GrantManagerManagement.Default used to give access to the Applications menu, the
            // Payments menu and application assignment. Copy every role and user grant of it, in
            // every tenant, to the permissions that now control those areas, then remove it.
            migrationBuilder.Sql(
                @"INSERT INTO ""PermissionGrants"" (""Id"", ""TenantId"", ""Name"", ""ProviderName"", ""ProviderKey"")
                SELECT gen_random_uuid(), d.""TenantId"", p.""Name"", d.""ProviderName"", d.""ProviderKey""
                FROM ""PermissionGrants"" d
                CROSS JOIN (VALUES
                    ('GrantApplicationManagement.Applications'),
                    ('PaymentsPermissions.Payments'),
                    ('GrantApplicationManagement.Assignments'),
                    ('GrantApplicationManagement.Assignments.AssignInitial')
                ) AS p(""Name"")
                WHERE d.""Name"" = 'GrantManagerManagement.Default'
                  AND NOT EXISTS (
                      SELECT 1 FROM ""PermissionGrants"" e
                      WHERE e.""TenantId"" IS NOT DISTINCT FROM d.""TenantId""
                        AND e.""Name"" = p.""Name""
                        AND e.""ProviderName"" = d.""ProviderName""
                        AND e.""ProviderKey"" = d.""ProviderKey"");");

            migrationBuilder.Sql(
                @"DELETE FROM ""PermissionGrants""
                WHERE ""Name"" = 'GrantManagerManagement.Default';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Not reversible: after the migration there is no way to tell which grants of the
            // replacement permissions came from GrantManagerManagement.Default.
        }
    }
}
