CREATE OR REPLACE PROCEDURE "Reporting".generate_worksheet_view(IN correlation_id uuid)
 LANGUAGE plpgsql
AS $procedure$
DECLARE
    view_name TEXT;
    mapping_data JSONB;
    mapping_rows JSONB;
    data_clause TEXT;
    report_map_id UUID;
BEGIN
    -- Fetch the view name, mapping data, and ID from ReportColumnsMap for worksheet provider
    SELECT "Id", "ViewName", "Mapping"::JSONB INTO report_map_id, view_name, mapping_data
    FROM "Reporting"."ReportColumnsMaps"
    WHERE "CorrelationId" = correlation_id AND "CorrelationProvider" = 'worksheet';
    
    -- Check if we found the mapping
    IF mapping_data IS NULL OR report_map_id IS NULL THEN
        RAISE EXCEPTION 'No worksheet mapping found for CorrelationId: %', correlation_id;
    END IF;
    
    -- Generate a view name if it's empty or null
    IF view_name IS NULL OR trim(view_name) = '' THEN
        view_name := 'worksheet_view_' || replace(correlation_id::text, '-', '_');
    END IF;
    
    -- Extract the rows array from mapping data to validate
    mapping_rows := mapping_data->'Rows';
    
    -- Check if rows exist
    IF mapping_rows IS NULL OR jsonb_array_length(mapping_rows) = 0 THEN
        RAISE EXCEPTION 'No mapping rows found for CorrelationId: %', correlation_id;
    END IF;
    
    -- Get the data clause from the worksheet data provider using the report map ID
    data_clause := "Reporting".get_worksheet_data(correlation_id, report_map_id);
    
    -- Debug: Show what we're about to execute
    RAISE NOTICE 'View name: %', view_name;
    RAISE NOTICE 'Data clause: %', data_clause;
    
    -- Drop the view if it already exists to avoid conflicts
    EXECUTE format('DROP VIEW IF EXISTS "Reporting".%I', view_name);
    
    -- Create the view query dynamically - data_clause already contains complete SELECT query
    EXECUTE format('CREATE VIEW "Reporting".%I AS %s',
        view_name,
        data_clause
    );
    
    RAISE NOTICE 'Successfully created worksheet view: %', view_name;
END;
$procedure$;