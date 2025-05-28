using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Unity.GrantManager.Migrations.TenantMigrations
{
    /// <inheritdoc />
    public partial class Total_Score_To_Scoresheets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE OR REPLACE PROCEDURE \"Reporting\".generate_scoresheets_view(table_a_id UUID) AS $$\r\nDECLARE\r\n    view_name TEXT;\r\n    view_columns TEXT;\r\n    view_keys TEXT;\r\n    column_definitions TEXT;\r\n    column_names TEXT[];\r\n    key_names TEXT[];\r\n    select_clause TEXT;\r\nBEGIN\r\n    -- Fetch the name, columns, and keys from Table A\r\n    SELECT \"ReportViewName\", \"ReportColumns\", \"ReportKeys\" INTO view_name, view_columns, view_keys\r\n    FROM \"Flex\".\"Scoresheets\"\r\n    WHERE \"Id\" = table_a_id;\r\n\r\n    -- Split the columns and keys into individual names using pipe delimiter\r\n    column_names := string_to_array(view_columns, '|');\r\n    key_names := string_to_array(view_keys, '|');\r\n\r\n    -- Create the column definitions for the view\r\n    column_definitions := '';\r\n    FOR i IN 1..array_length(column_names, 1) LOOP\r\n        column_definitions := column_definitions || format('%I TEXT', column_names[i]);\r\n        IF i < array_length(column_names, 1) THEN\r\n            column_definitions := column_definitions || ', ';\r\n        END IF;\r\n    END LOOP;\r\n\r\n    -- Create the select clause for the view\r\n    select_clause := '';\r\n    FOR i IN 1..array_length(key_names, 1) LOOP\r\n        IF i > 1 THEN\r\n            select_clause := select_clause || ', ';\r\n        END IF;\r\n        select_clause := select_clause || format(\r\n            'COALESCE((\r\n                SELECT value::TEXT\r\n                FROM jsonb_each_text(\"ReportData\")\r\n                WHERE key = %L\r\n                LIMIT 1\r\n            ), '''') AS %I',\r\n            key_names[i], column_names[i]\r\n        );\r\n    END LOOP;\r\n\r\n    -- Drop the view if it already exists to avoid type conflicts\r\n    EXECUTE format('DROP VIEW IF EXISTS %I', view_name);\r\n\r\n    -- Create the view query dynamically\r\n    EXECUTE format('CREATE VIEW \"Reporting\".%I AS SELECT \"Id\", \"CorrelationId\", \"CorrelationProvider\", \"TotalScore\", %s FROM (SELECT \"Id\", \"CorrelationId\", \"CorrelationProvider\", COALESCE((\"ReportData\"->>''TotalScore'')::integer, 0) as \"TotalScore\", \"ReportData\" FROM \"Flex\".\"ScoresheetInstances\" WHERE \"Flex\".\"ScoresheetInstances\".\"ScoresheetId\" = %L) AS subquery',\r\n        view_name,\r\n        select_clause,\r\n        table_a_id\r\n    );\r\nEND;\r\n$$ LANGUAGE plpgsql;\r\n");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
