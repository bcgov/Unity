using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.IO;
using System.Reflection;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AddSafeToJsonbAndGuardDataGridLateral : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var assembly = Assembly.GetExecutingAssembly();

            ApplyScript(migrationBuilder, assembly, "Unity.GrantManager.Scripts.safe_to_jsonb.sql");
            ApplyScript(migrationBuilder, assembly, "Unity.GrantManager.Scripts.get_worksheet_data.sql");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }

        private static void ApplyScript(MigrationBuilder migrationBuilder, Assembly assembly, string resourceName)
        {
            using Stream stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
            using StreamReader reader = new(stream);
            string sql = reader.ReadToEnd();
            migrationBuilder.Sql(sql);
        }
    }
}
