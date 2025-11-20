using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.IO;
using System.Reflection;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class AddScoresheetViewGeneration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create the total score calculation function first (dependency for the data function)
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Unity.GrantManager.Scripts.calculate_scoresheet_total_score.sql";

            using Stream stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
            using StreamReader reader = new StreamReader(stream);
            string sql = reader.ReadToEnd();
            migrationBuilder.Sql(sql);

            // Create the data provider function for scoresheets
            var resourceName2 = "Unity.GrantManager.Scripts.get_scoresheet_data.sql";

            using Stream stream2 = assembly.GetManifestResourceStream(resourceName2)
                ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName2}");
            using StreamReader reader2 = new StreamReader(stream2);
            string sql2 = reader2.ReadToEnd();
            migrationBuilder.Sql(sql2);

            // Create the main view generation procedure for scoresheets           
            var resourceName3 = "Unity.GrantManager.Scripts.generate_scoresheet_view.sql";

            using Stream stream3 = assembly.GetManifestResourceStream(resourceName3)
                ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName3}");
            using StreamReader reader3 = new StreamReader(stream3);
            string sql3 = reader3.ReadToEnd();
            migrationBuilder.Sql(sql3);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the procedure, function, and total score function
            migrationBuilder.Sql(@"DROP PROCEDURE IF EXISTS ""Reporting"".generate_scoresheet_view(UUID);");
            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS ""Reporting"".get_scoresheet_data(UUID, UUID);");
            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS ""Reporting"".calculate_scoresheet_total_score(UUID);");
        }
    }
}
