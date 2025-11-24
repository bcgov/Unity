using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.IO;
using System.Reflection;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AddWorksheetViewGeneration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create the data provider function for worksheets
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Unity.GrantManager.Scripts.get_worksheet_data.sql";

            using Stream stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
            using StreamReader reader = new StreamReader(stream);
            string sql = reader.ReadToEnd();
            migrationBuilder.Sql(sql);

            // Create the main view generation procedure for worksheets           
            var resourceName2 = "Unity.GrantManager.Scripts.generate_worksheet_view.sql";

            using Stream stream2 = assembly.GetManifestResourceStream(resourceName2)
                ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName2}");
            using StreamReader reader2 = new StreamReader(stream2);
            string sql2 = reader2.ReadToEnd();
            migrationBuilder.Sql(sql2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the new procedure and function
            migrationBuilder.Sql(@"DROP PROCEDURE IF EXISTS ""Reporting"".generate_worksheet_view(UUID);");
            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS ""Reporting"".get_worksheet_data(UUID, UUID);");
        }
    }
}
