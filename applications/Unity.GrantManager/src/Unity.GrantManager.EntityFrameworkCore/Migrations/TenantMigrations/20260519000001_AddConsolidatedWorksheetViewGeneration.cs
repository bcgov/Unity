using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.IO;
using System.Reflection;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AddConsolidatedWorksheetViewGeneration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var assembly = Assembly.GetExecutingAssembly();

            ApplyScript(migrationBuilder, assembly, "Unity.GrantManager.Scripts.get_consolidated_worksheet_data.sql");
            ApplyScript(migrationBuilder, assembly, "Unity.GrantManager.Scripts.generate_consolidated_worksheet_view.sql");
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
