using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.IO;
using System.Reflection;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class UpdateScoresheetViewGenFunction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create the total score calculation function first (dependency for the data function)
            var assembly = Assembly.GetExecutingAssembly();

            // Create the data provider function for scoresheets
            var resourceName = "Unity.GrantManager.Scripts.get_scoresheet_data.sql";

            using Stream stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Could not find embedded resource: {resourceName}");
            using StreamReader reader = new StreamReader(stream);
            string sql = reader.ReadToEnd();
            migrationBuilder.Sql(sql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
