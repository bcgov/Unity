using System;
using System.IO;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AB33799_HardenCheckboxGroupReportingViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Re-deploy get_worksheet_data / get_consolidated_worksheet_data with a guard around the
            // checkbox-group ::jsonb cast: a stored value that is null, '', or otherwise not a JSON
            // array (e.g. from a checkbox-group field saved with zero selections) now resolves to
            // NULL for that column instead of raising "invalid input syntax for type json" and
            // breaking the whole generated view. CREATE OR REPLACE is idempotent.
            RunEmbeddedScript(migrationBuilder, "get_worksheet_data.sql");
            RunEmbeddedScript(migrationBuilder, "get_consolidated_worksheet_data.sql");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: the prior function bodies are not preserved: refer to source control history
            // for the pre-hardening SQL if a rollback is ever required.
        }

        private static void RunEmbeddedScript(MigrationBuilder migrationBuilder, string scriptFileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"Unity.GrantManager.Scripts.{scriptFileName}";

            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
            using var reader = new StreamReader(stream);
            migrationBuilder.Sql(reader.ReadToEnd());
        }
    }
}
