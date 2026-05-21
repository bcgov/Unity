CREATE OR REPLACE PROCEDURE "Reporting".generate_consolidated_worksheet_view(IN form_id uuid)
 LANGUAGE plpgsql
AS $procedure$
DECLARE
    view_name TEXT;
    mapping_data JSONB;
    mapping_rows JSONB;
    data_clause TEXT;
    report_map_id UUID;
BEGIN
    -- Fetch the view name, mapping data, and ID from ReportColumnsMap for worksheetconsolidated provider
    SELECT "Id", "ViewName", "Mapping"::JSONB INTO report_map_id, view_name, mapping_data
    FROM "Reporting"."ReportColumnsMaps"
    WHERE "CorrelationId" = form_id AND "CorrelationProvider" = 'worksheetconsolidated';

    -- Check if we found the mapping
    IF mapping_data IS NULL OR report_map_id IS NULL THEN
        RAISE EXCEPTION 'No consolidated worksheet mapping found for FormId: %', form_id;
    END IF;

    -- Generate a view name if it is empty or null
    IF view_name IS NULL OR trim(view_name) = '' THEN
        view_name := 'consolidated_worksheet_view_' || replace(form_id::text, '-', '_');
    END IF;

    -- Extract the rows array from mapping data to validate
    mapping_rows := mapping_data->'Rows';

    -- Check if rows exist
    IF mapping_rows IS NULL OR jsonb_array_length(mapping_rows) = 0 THEN
        RAISE EXCEPTION 'No mapping rows found for consolidated worksheet FormId: %', form_id;
    END IF;

    -- Get the data clause from the consolidated worksheet data function
    data_clause := "Reporting".get_consolidated_worksheet_data(form_id, report_map_id);

    RAISE NOTICE 'View name: %', view_name;
    RAISE NOTICE 'Data clause: %', data_clause;

    -- Drop existing view to avoid conflicts on regeneration
    EXECUTE format('DROP VIEW IF EXISTS "Reporting".%I', view_name);

    -- Create the view
    EXECUTE format('CREATE VIEW "Reporting".%I AS %s',
        view_name,
        data_clause
    );

    RAISE NOTICE 'Successfully created consolidated worksheet view: %', view_name;
END;
$procedure$;
