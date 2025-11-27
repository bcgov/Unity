using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.IO;
using System.Reflection;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class UpdateViewGenCurrencyPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create the main view generation procedure            
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Unity.GrantManager.Scripts.get_formversion_data.sql";

            using Stream stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
            using StreamReader reader = new(stream);
            string sql = reader.ReadToEnd();
            migrationBuilder.Sql(sql);
            
            var resourceName2 = "Unity.GrantManager.Scripts.get_worksheet_data.sql";

            using Stream stream2 = assembly.GetManifestResourceStream(resourceName2)
                ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName2}");
            using StreamReader reader2 = new(stream2);
            string sql2 = reader2.ReadToEnd();
            migrationBuilder.Sql(sql2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
