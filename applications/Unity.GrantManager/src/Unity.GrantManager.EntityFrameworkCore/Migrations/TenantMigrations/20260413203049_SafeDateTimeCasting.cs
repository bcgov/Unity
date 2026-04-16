using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.IO;
using System.Reflection;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class SafeDateTimeCasting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var assembly = Assembly.GetExecutingAssembly();

            // Create the safe_to_timestamp helper function
            var safeTimestampResource = "Unity.GrantManager.Scripts.safe_to_timestamp.sql";
            using Stream stream1 = assembly.GetManifestResourceStream(safeTimestampResource)
                ?? throw new InvalidOperationException($"Could not find embedded resource: {safeTimestampResource}");
            using StreamReader reader1 = new StreamReader(stream1);
            string sql1 = reader1.ReadToEnd();
            migrationBuilder.Sql(sql1);

            // Create the safe_to_date helper function
            var safeDateResource = "Unity.GrantManager.Scripts.safe_to_date.sql";
            using Stream stream2 = assembly.GetManifestResourceStream(safeDateResource)
                ?? throw new InvalidOperationException($"Could not find embedded resource: {safeDateResource}");
            using StreamReader reader2 = new StreamReader(stream2);
            string sql2 = reader2.ReadToEnd();
            migrationBuilder.Sql(sql2);

            // Update the get_worksheet_data function to use safe casting helpers
            var worksheetDataResource = "Unity.GrantManager.Scripts.get_worksheet_data.sql";
            using Stream stream3 = assembly.GetManifestResourceStream(worksheetDataResource)
                ?? throw new InvalidOperationException($"Could not find embedded resource: {worksheetDataResource}");
            using StreamReader reader3 = new StreamReader(stream3);
            string sql3 = reader3.ReadToEnd();
            migrationBuilder.Sql(sql3);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS ""Reporting"".safe_to_timestamp(TEXT);");
            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS ""Reporting"".safe_to_date(TEXT);");
        }
    }
}
