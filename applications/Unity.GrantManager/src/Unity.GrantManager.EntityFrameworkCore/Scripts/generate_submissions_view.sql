CREATE OR REPLACE PROCEDURE "Reporting".generate_submissions_view(IN table_a_id uuid)
    LANGUAGE plpgsql
    AS $$
DECLARE
    view_name TEXT;
    view_columns TEXT;
    view_keys TEXT;
    column_definitions TEXT;
    column_names TEXT[];
    key_names TEXT[];
    select_clause TEXT;
BEGIN
    -- Fetch the name, columns, and keys from Table A
    SELECT "ReportViewName", "ReportColumns", "ReportKeys" INTO view_name, view_columns, view_keys
    FROM "public"."ApplicationFormVersion"
    WHERE "Id" = table_a_id;

    -- Split the columns and keys into individual names using pipe delimiter
    column_names := string_to_array(view_columns, '|');
    key_names := string_to_array(view_keys, '|');

    -- Create the column definitions for the view
    column_definitions := '';
    FOR i IN 1..array_length(column_names, 1) LOOP
        column_definitions := column_definitions || format('%I TEXT', column_names[i]);
        IF i < array_length(column_names, 1) THEN
            column_definitions := column_definitions || ', ';
        END IF;
    END LOOP;

    -- Create the select clause for the view
    select_clause := '';
    FOR i IN 1..array_length(key_names, 1) LOOP
        IF i > 1 THEN
            select_clause := select_clause || ', ';
        END IF;
        select_clause := select_clause || format(
            'COALESCE((
                SELECT value::TEXT
                FROM jsonb_each_text("ReportData")
                WHERE key = %L
                LIMIT 1
            ), '''') AS %I',
            key_names[i], column_names[i]
        );
    END LOOP;

    -- Drop the view if it already exists to avoid type conflicts
    EXECUTE format('DROP VIEW IF EXISTS %I', view_name);

    -- Create the view query dynamically
    EXECUTE format('CREATE VIEW "Reporting".%I AS SELECT "Id", "ApplicationId", %s FROM (SELECT "Id", "ApplicationId", "ReportData" FROM "public"."ApplicationFormSubmissions" WHERE "public"."ApplicationFormSubmissions"."ApplicationFormVersionId" = %L) AS subquery',
        view_name,
        select_clause,
        table_a_id
    );
END;
$$;
