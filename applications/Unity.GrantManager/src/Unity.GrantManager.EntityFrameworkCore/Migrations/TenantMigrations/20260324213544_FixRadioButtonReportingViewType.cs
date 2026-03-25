using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.IO;
using System.Reflection;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class FixRadioButtonReportingViewType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Re-execute the get_formversion_data function to fix radio button type handling.
            // Radio buttons now use TEXT instead of BOOLEAN, matching the CHEFS data model
            // where radio values are strings (e.g., "email") not booleans.
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Unity.GrantManager.Scripts.get_formversion_data.sql";

            using Stream stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
            using StreamReader reader = new(stream);
            string sql = reader.ReadToEnd();
            migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
