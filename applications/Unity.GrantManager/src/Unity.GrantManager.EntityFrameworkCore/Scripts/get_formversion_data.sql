CREATE OR REPLACE FUNCTION "Reporting".get_formversion_data(correlation_id uuid, report_map_id uuid)
 RETURNS text
 LANGUAGE plpgsql
AS $function$
DECLARE
    mapping_rows JSONB;
    row_data JSONB;
    column_name TEXT;
    column_type TEXT;
    property_name TEXT;
    data_path_raw TEXT;
    type_path TEXT;
    datagrid_name TEXT;
    datagrid_id TEXT;
    base_select_clause TEXT;
    legacy_select_clause TEXT;
    current_select_clause TEXT;
    legacy_from_clause TEXT;
    current_from_clause TEXT;
    data_path TEXT;
    json_path TEXT;
    path_parts TEXT[];
    field_name TEXT;
    has_datagrids BOOLEAN := false;
    has_root_fields BOOLEAN := false;
    unique_datagrids JSONB := '{}';
    all_columns JSONB := '{}';
    column_type_conflicts JSONB := '{}';
    legacy_source_prefix TEXT;
    current_source_prefix TEXT;
    final_query TEXT;
    datagrid_queries TEXT[];
    root_query TEXT;
    column_list TEXT;
    use_text_fallback BOOLEAN := false;
    i INTEGER;
    j INTEGER;
BEGIN
    -- Fetch the mapping data for this report map ID
    SELECT "Mapping"->'Rows' INTO mapping_rows
    FROM "Reporting"."ReportColumnsMaps"
    WHERE "Id" = report_map_id;
    
    -- Check if we found the mapping rows
    IF mapping_rows IS NULL OR jsonb_array_length(mapping_rows) = 0 THEN
        RAISE EXCEPTION 'No mapping rows found for ReportColumnsMaps ID: %', report_map_id;
    END IF;
    
    -- First pass: collect all unique column names and detect type conflicts
    FOR i IN 0..(jsonb_array_length(mapping_rows) - 1) LOOP
        row_data := mapping_rows->i;
        
        -- Skip parent rows
        IF (row_data->>'Parent')::boolean = true THEN
            CONTINUE;
        END IF;
        
        column_name := row_data->>'ColumnName';
        column_type := row_data->>'Type';
        type_path := row_data->>'TypePath';
        data_path_raw := row_data->>'DataPath';
        datagrid_id := row_data->>'Id';
        
        -- Check for type conflicts with existing columns
        IF all_columns ? column_name THEN
            -- Column already exists, check if types match
            IF (all_columns->column_name->>'type') != column_type THEN
                -- Type conflict detected - mark for TEXT fallback
                column_type_conflicts := column_type_conflicts || jsonb_build_object(column_name, true);
                use_text_fallback := true;
            END IF;
        END IF;
        
        -- Store column info
        all_columns := all_columns || jsonb_build_object(column_name, jsonb_build_object(
            'type', column_type,
            'type_path', COALESCE(type_path, ''),
            'data_path', COALESCE(data_path_raw, '')
        ));
        
        -- Check if this is a datagrid field (TypePath contains 'datagrid')
        IF type_path IS NOT NULL AND type_path LIKE '%datagrid%' THEN
            has_datagrids := true;
            
            -- Extract datagrid name from DataPath field
            -- Handle both patterns: "(DK1)dataGrid->field" and "dataGridName->field"
            IF data_path_raw IS NOT NULL THEN
                IF data_path_raw ~ '^\(DK[0-9]+\)' THEN
                    -- Remove the DK prefix and extract the datagrid name
                    datagrid_name := regexp_replace(data_path_raw, '^\(DK[0-9]+\)', '');
                    datagrid_name := split_part(datagrid_name, '->', 1);
                ELSE
                    -- Direct format without DK prefix
                    datagrid_name := split_part(data_path_raw, '->', 1);
                END IF;
                
                -- Store unique datagrids with their IDs (use first ID found for each datagrid name)
                IF datagrid_name IS NOT NULL AND datagrid_name != '' AND NOT (unique_datagrids ? datagrid_name) THEN
                    unique_datagrids := unique_datagrids || jsonb_build_object(datagrid_name, datagrid_id);
                END IF;
            END IF;
        ELSE
            has_root_fields := true;
        END IF;
    END LOOP;
    
    -- Build standardized column list with appropriate types
    column_list := '';
    FOR column_name IN SELECT jsonb_object_keys(all_columns) ORDER BY jsonb_object_keys(all_columns) LOOP
        IF column_list != '' THEN
            column_list := column_list || ', ';
        END IF;
        
        -- Use TEXT only if there's a type conflict for this column
        IF use_text_fallback AND (column_type_conflicts ? column_name) THEN
            column_list := column_list || format('NULL::TEXT AS %I', column_name);
        ELSE
            -- Use the original type for the column
            column_type := all_columns->column_name->>'type';
            CASE column_type
                WHEN 'number' THEN
                    column_list := column_list || format('NULL::NUMERIC AS %I', column_name);
                WHEN 'currency' THEN
                    column_list := column_list || format('NULL::DECIMAL(18,2) AS %I', column_name);
                WHEN 'option', 'checkbox', 'radio' THEN
                    column_list := column_list || format('NULL::BOOLEAN AS %I', column_name);
                ELSE
                    column_list := column_list || format('NULL::TEXT AS %I', column_name);
            END CASE;
        END IF;
    END LOOP;
    
    -- Initialize arrays for storing queries
    datagrid_queries := '{}';
    
    -- Create root query if we have root fields
    IF has_root_fields THEN
        -- Initialize base select clause with common fields for root query
        base_select_clause := 'afs."Id" AS submission_id, afs."ApplicationId" AS application_id, ''root'' AS row_identifier';
        
        -- Add all columns as placeholders first
        IF column_list != '' THEN
            base_select_clause := base_select_clause || ', ' || column_list;
        END IF;
        
        -- Initialize FROM clauses for both schemas
        legacy_from_clause := 'public."ApplicationFormSubmissions" afs';
        current_from_clause := 'public."ApplicationFormSubmissions" afs';
        
        -- Initialize select clauses for both schemas
        legacy_select_clause := base_select_clause;
        current_select_clause := base_select_clause;
        
        -- Process root fields only - replace NULL placeholders with actual values
        FOR i IN 0..(jsonb_array_length(mapping_rows) - 1) LOOP
            row_data := mapping_rows->i;
            
            -- Skip parent rows and datagrid rows
            IF (row_data->>'Parent')::boolean = true OR 
               (row_data->>'TypePath') IS NOT NULL AND (row_data->>'TypePath') LIKE '%datagrid%' THEN
                CONTINUE;
            END IF;
            
            column_name := row_data->>'ColumnName';
            column_type := row_data->>'Type';
            property_name := row_data->>'PropertyName';
            data_path_raw := row_data->>'DataPath';
            
            -- Root field - different paths for legacy vs current schema
            IF data_path_raw IS NOT NULL AND trim(data_path_raw) != '' THEN
                path_parts := string_to_array(data_path_raw, '->');
                json_path := '';
                FOR j IN 1..array_length(path_parts, 1) LOOP
                    IF trim(path_parts[j]) = '' THEN
                        CONTINUE;
                    END IF;
                    
                    IF json_path != '' THEN
                        json_path := json_path || '->';
                    END IF;
                    json_path := json_path || format('''%s''', trim(path_parts[j]));
                END LOOP;
                json_path := '->' || json_path;
            ELSE
                json_path := format('->''%s''', property_name);
            END IF;
            
            -- Different source prefixes for legacy vs current schema
            legacy_source_prefix := 'afs."Submission"->''submission''->''data''';
            current_source_prefix := 'afs."Submission"->''submission''->''submission''->''data''';
            
            -- Apply type-specific extraction - use TEXT fallback only if there's a conflict
            IF use_text_fallback AND (column_type_conflicts ? column_name) THEN
                -- Force TEXT for conflicted columns
                CASE column_type
                    WHEN 'textfield', 'textarea', 'email', 'select', 'phoneNumber' THEN
                        IF json_path LIKE '%->%' THEN
                            json_path := regexp_replace(json_path, '->([^>]+)$', '->>\1');
                        ELSE
                            json_path := replace(json_path, '->', '->>');
                        END IF;
                        data_path := format('(%s%s)::TEXT', '{}', json_path);
                        
                    WHEN 'number' THEN
                        IF json_path LIKE '%->%' THEN
                            json_path := regexp_replace(json_path, '->([^>]+)$', '->>\1');
                        ELSE
                            json_path := replace(json_path, '->', '->>');
                        END IF;
                        data_path := format('(CASE 
                            WHEN (%s%s) IS NULL THEN NULL
                            WHEN (%s%s) ~ ''^-?[0-9]+\.?[0-9]*$'' THEN (%s%s)::NUMERIC::TEXT
                            ELSE (%s%s)::TEXT
                        END)', '{}', json_path, '{}', json_path, '{}', json_path, '{}', json_path);
                        
                    WHEN 'currency' THEN
                        IF json_path LIKE '%->%' THEN
                            json_path := regexp_replace(json_path, '->([^>]+)$', '->>\1');
                        ELSE
                            json_path := replace(json_path, '->', '->>');
                        END IF;
                        data_path := format('(CASE 
                            WHEN (%s%s) IS NULL THEN NULL
                            WHEN (%s%s) ~ ''^-?[0-9]+\.?[0-9]*$'' THEN (%s%s)::DECIMAL(18,2)::TEXT
                            ELSE (%s%s)::TEXT
                        END)', '{}', json_path, '{}', json_path, '{}', json_path, '{}', json_path);
                        
                    WHEN 'option', 'checkbox', 'radio' THEN
                        IF json_path LIKE '%->%' THEN
                            json_path := regexp_replace(json_path, '->([^>]+)$', '->>\1');
                        ELSE
                            json_path := replace(json_path, '->', '->>');
                        END IF;
                        data_path := format('(CASE 
                            WHEN (%s%s) IS NULL THEN NULL
                            WHEN (%s%s) ~ ''^(true|false|t|f|1|0|yes|no)$'' THEN 
                                CASE 
                                    WHEN lower((%s%s)) IN (''true'', ''t'', ''1'', ''yes'') THEN ''true''
                                    WHEN lower((%s%s)) IN (''false'', ''f'', ''0'', ''no'') THEN ''false''
                                    ELSE (%s%s)::TEXT
                            END
                            ELSE (%s%s)::TEXT
                        END)', '{}', json_path, '{}', json_path, '{}', json_path, '{}', json_path, '{}', json_path, '{}', json_path);
                        
                    ELSE
                        -- Default to text
                        IF json_path LIKE '%->%' THEN
                            json_path := regexp_replace(json_path, '->([^>]+)$', '->>\1');
                        ELSE
                            json_path := replace(json_path, '->', '->>');
                        END IF;
                        data_path := format('(%s%s)::TEXT', '{}', json_path);
                END CASE;
            ELSE
                -- Use original types (no conflicts)
                CASE column_type
                    WHEN 'textfield', 'textarea', 'email', 'select', 'phoneNumber' THEN
                        IF json_path LIKE '%->%' THEN
                            json_path := regexp_replace(json_path, '->([^>]+)$', '->>\1');
                        ELSE
                            json_path := replace(json_path, '->', '->>');
                        END IF;
                        data_path := format('(%s%s)', '{}', json_path);
                        
                    WHEN 'number' THEN
                        IF json_path LIKE '%->%' THEN
                            json_path := regexp_replace(json_path, '->([^>]+)$', '->>\1');
                        ELSE
                            json_path := replace(json_path, '->', '->>');
                        END IF;
                        data_path := format('(CASE 
                            WHEN (%s%s) IS NULL THEN NULL
                            WHEN (%s%s) ~ ''^-?[0-9]+\.?[0-9]*$'' THEN (%s%s)::NUMERIC
                            ELSE NULL
                        END)', '{}', json_path, '{}', json_path, '{}', json_path);
                        
                    WHEN 'currency' THEN
                        IF json_path LIKE '%->%' THEN
                            json_path := regexp_replace(json_path, '->([^>]+)$', '->>\1');
                        ELSE
                            json_path := replace(json_path, '->', '->>');
                        END IF;
                        data_path := format('(CASE 
                            WHEN (%s%s) IS NULL THEN NULL
                            WHEN (%s%s) ~ ''^-?[0-9]+\.?[0-9]*$'' THEN (%s%s)::DECIMAL(18,2)
                            ELSE NULL
                        END)', '{}', json_path, '{}', json_path, '{}', json_path);
                        
                    WHEN 'option', 'checkbox', 'radio' THEN
                        IF json_path LIKE '%->%' THEN
                            json_path := regexp_replace(json_path, '->([^>]+)$', '->>\1');
                        ELSE
                            json_path := replace(json_path, '->', '->>');
                        END IF;
                        data_path := format('(CASE 
                            WHEN (%s%s) IS NULL THEN NULL
                            WHEN (%s%s) ~ ''^(true|false|t|f|1|0|yes|no)$'' THEN 
                                CASE 
                                    WHEN lower((%s%s)) IN (''true'', ''t'', ''1'', ''yes'') THEN true
                                    WHEN lower((%s%s)) IN (''false'', ''f'', ''0'', ''no'') THEN false
                                    ELSE NULL
                                END
                            ELSE NULL
                        END)', '{}', json_path, '{}', json_path, '{}', json_path, '{}', json_path);
                        
                    ELSE
                        -- Default to text
                        IF json_path LIKE '%->%' THEN
                            json_path := regexp_replace(json_path, '->([^>]+)$', '->>\1');
                        ELSE
                            json_path := replace(json_path, '->', '->>');
                        END IF;
                        data_path := format('(%s%s)', '{}', json_path);
                END CASE;
            END IF;
            
            -- Replace the NULL placeholder with the actual value
            IF use_text_fallback AND (column_type_conflicts ? column_name) THEN
                legacy_select_clause := replace(legacy_select_clause, 
                    format('NULL::TEXT AS %I', column_name),
                    format('%s AS %I', replace(data_path, '{}', legacy_source_prefix), column_name)
                );
                current_select_clause := replace(current_select_clause,
                    format('NULL::TEXT AS %I', column_name),
                    format('%s AS %I', replace(data_path, '{}', current_source_prefix), column_name)
                );
            ELSE
                -- Replace with proper typed placeholder
                CASE column_type
                    WHEN 'number' THEN
                        legacy_select_clause := replace(legacy_select_clause,
                            format('NULL::NUMERIC AS %I', column_name),
                            format('%s AS %I', replace(data_path, '{}', legacy_source_prefix), column_name)
                        );
                        current_select_clause := replace(current_select_clause,
                            format('NULL::NUMERIC AS %I', column_name),
                            format('%s AS %I', replace(data_path, '{}', current_source_prefix), column_name)
                        );
                    WHEN 'currency' THEN
                        legacy_select_clause := replace(legacy_select_clause,
                            format('NULL::DECIMAL(18,2) AS %I', column_name),
                            format('%s AS %I', replace(data_path, '{}', legacy_source_prefix), column_name)
                        );
                        current_select_clause := replace(current_select_clause,
                            format('NULL::DECIMAL(18,2) AS %I', column_name),
                            format('%s AS %I', replace(data_path, '{}', current_source_prefix), column_name)
                        );
                    WHEN 'option', 'checkbox', 'radio' THEN
                        legacy_select_clause := replace(legacy_select_clause,
                            format('NULL::BOOLEAN AS %I', column_name),
                            format('%s AS %I', replace(data_path, '{}', legacy_source_prefix), column_name)
                        );
                        current_select_clause := replace(current_select_clause,
                            format('NULL::BOOLEAN AS %I', column_name),
                            format('%s AS %I', replace(data_path, '{}', current_source_prefix), column_name)
                        );
                    ELSE
                        legacy_select_clause := replace(legacy_select_clause,
                            format('NULL::TEXT AS %I', column_name),
                            format('%s AS %I', replace(data_path, '{}', legacy_source_prefix), column_name)
                        );
                        current_select_clause := replace(current_select_clause,
                            format('NULL::TEXT AS %I', column_name),
                            format('%s AS %I', replace(data_path, '{}', current_source_prefix), column_name)
                        );
                END CASE;
            END IF;
        END LOOP;
        
        -- Build root query
        root_query := format('(SELECT %s FROM %s WHERE afs."ApplicationFormVersionId" = %L AND afs."Submission"->''submission''->''submission'' IS NULL) UNION ALL (SELECT %s FROM %s WHERE afs."ApplicationFormVersionId" = %L AND afs."Submission"->''submission''->''submission'' IS NOT NULL)',
            legacy_select_clause,
            legacy_from_clause,
            correlation_id,
            current_select_clause,
            current_from_clause,
            correlation_id
        );
    END IF;
    
    -- Create separate query for each datagrid
    IF has_datagrids THEN
        FOR datagrid_name IN SELECT jsonb_object_keys(unique_datagrids) LOOP
            datagrid_id := unique_datagrids->>datagrid_name;
            
            -- Initialize base select clause with common fields for this datagrid
            base_select_clause := format('afs."Id" AS submission_id, afs."ApplicationId" AS application_id, %L || ''_r'' || dg_%s_tbl.dg_%s_row_num AS row_identifier',
                datagrid_name, datagrid_id, datagrid_id);
            
            -- Add all columns as placeholders first
            IF column_list != '' THEN
                base_select_clause := base_select_clause || ', ' || column_list;
            END IF;
            
            -- Initialize FROM clauses for both schemas with CROSS JOIN for this specific datagrid
            legacy_from_clause := format('public."ApplicationFormSubmissions" afs CROSS JOIN LATERAL (
                SELECT elem AS dg_%s, row_number() OVER() AS dg_%s_row_num
                FROM jsonb_array_elements(
                    COALESCE(
                        afs."Submission"->''submission''->''data''->''%s'',
                        ''[null]''::jsonb
                    )
                ) AS elem
            ) AS dg_%s_tbl', datagrid_id, datagrid_id, datagrid_name, datagrid_id);
            
            current_from_clause := format('public."ApplicationFormSubmissions" afs CROSS JOIN LATERAL (
                SELECT elem AS dg_%s, row_number() OVER() AS dg_%s_row_num
                FROM jsonb_array_elements(
                    COALESCE(
                        afs."Submission"->''submission''->''submission''->''data''->''%s'',
                        ''[null]''::jsonb
                    )
                ) AS elem
            ) AS dg_%s_tbl', datagrid_id, datagrid_id, datagrid_name, datagrid_id);
            
            -- Initialize select clauses for both schemas
            legacy_select_clause := base_select_clause;
            current_select_clause := base_select_clause;
            
            -- Process fields for this specific datagrid - replace NULL placeholders with actual values
            FOR i IN 0..(jsonb_array_length(mapping_rows) - 1) LOOP
                row_data := mapping_rows->i;
                
                -- Skip parent rows and non-datagrid rows
                IF (row_data->>'Parent')::boolean = true OR 
                   (row_data->>'TypePath') IS NULL OR NOT ((row_data->>'TypePath') LIKE '%datagrid%') THEN
                    CONTINUE;
                END IF;
                
                column_name := row_data->>'ColumnName';
                column_type := row_data->>'Type';
                data_path_raw := row_data->>'DataPath';
                
                -- Check if this field belongs to the current datagrid
                IF data_path_raw ~ '^\(DK[0-9]+\)' THEN
                    -- Remove the DK prefix and extract the datagrid name
                    IF split_part(regexp_replace(data_path_raw, '^\(DK[0-9]+\)', ''), '->', 1) != datagrid_name THEN
                        CONTINUE;
                    END IF;
                    field_name := split_part(regexp_replace(data_path_raw, '^\(DK[0-9]+\)', ''), '->', 2);
                ELSE
                    -- Direct format
                    IF split_part(data_path_raw, '->', 1) != datagrid_name THEN
                        CONTINUE;
                    END IF;
                    field_name := split_part(data_path_raw, '->', 2);
                END IF;
                
                json_path := format('->''%s''', trim(field_name));
                -- Same source prefix for both schemas since datagrids use lateral joins
                legacy_source_prefix := 'dg_' || datagrid_id || '_tbl.dg_' || datagrid_id;
                current_source_prefix := 'dg_' || datagrid_id || '_tbl.dg_' || datagrid_id;
                
                -- Apply same type handling logic as root fields
                IF use_text_fallback AND (column_type_conflicts ? column_name) THEN
                    -- Use TEXT fallback logic (same as root fields)
                    CASE column_type
                        WHEN 'textfield', 'textarea', 'email', 'select', 'phoneNumber' THEN
                            IF json_path LIKE '%->%' THEN
                                json_path := regexp_replace(json_path, '->([^>]+)$', '->>\1');
                            ELSE
                                json_path := replace(json_path, '->', '->>');
                            END IF;
                            data_path := format('(%s%s)::TEXT', '{}', json_path);
                        -- ... similar patterns for other types
                        ELSE
                            IF json_path LIKE '%->%' THEN
                                json_path := regexp_replace(json_path, '->([^>]+)$', '->>\1');
                            ELSE
                                json_path := replace(json_path, '->', '->>');
                            END IF;
                            data_path := format('(%s%s)::TEXT', '{}', json_path);
                    END CASE;
                    
                    -- Replace TEXT placeholder
                    legacy_select_clause := replace(legacy_select_clause,
                        format('NULL::TEXT AS %I', column_name),
                        format('%s AS %I', replace(data_path, '{}', legacy_source_prefix), column_name)
                    );
                    current_select_clause := replace(current_select_clause,
                        format('NULL::TEXT AS %I', column_name),
                        format('%s AS %I', replace(data_path, '{}', current_source_prefix), column_name)
                    );
                ELSE
                    -- Use original type logic (same as root fields)
                    CASE column_type
                        WHEN 'textfield', 'textarea', 'email', 'select', 'phoneNumber' THEN
                            IF json_path LIKE '%->%' THEN
                                json_path := regexp_replace(json_path, '->([^>]+)$', '->>\1');
                            ELSE
                                json_path := replace(json_path, '->', '->>');
                            END IF;
                            data_path := format('(%s%s)', '{}', json_path);
                            
                            legacy_select_clause := replace(legacy_select_clause,
                                format('NULL::TEXT AS %I', column_name),
                                format('%s AS %I', replace(data_path, '{}', legacy_source_prefix), column_name)
                            );
                            current_select_clause := replace(current_select_clause,
                                format('NULL::TEXT AS %I', column_name),
                                format('%s AS %I', replace(data_path, '{}', current_source_prefix), column_name)
                            );
                            
                        WHEN 'number' THEN
                            IF json_path LIKE '%->%' THEN
                                json_path := regexp_replace(json_path, '->([^>]+)$', '->>\1');
                            ELSE
                                json_path := replace(json_path, '->', '->>');
                            END IF;
                            data_path := format('(CASE 
                                WHEN (%s%s) IS NULL THEN NULL
                                WHEN (%s%s) ~ ''^-?[0-9]+\.?[0-9]*$'' THEN (%s%s)::NUMERIC
                                ELSE NULL
                            END)', '{}', json_path, '{}', json_path, '{}', json_path);
                            
                            legacy_select_clause := replace(legacy_select_clause,
                                format('NULL::NUMERIC AS %I', column_name),
                                format('%s AS %I', replace(data_path, '{}', legacy_source_prefix), column_name)
                            );
                            current_select_clause := replace(current_select_clause,
                                format('NULL::NUMERIC AS %I', column_name),
                                format('%s AS %I', replace(data_path, '{}', current_source_prefix), column_name)
                            );
                            
                        WHEN 'currency' THEN
                            IF json_path LIKE '%->%' THEN
                                json_path := regexp_replace(json_path, '->([^>]+)$', '->>\1');
                            ELSE
                                json_path := replace(json_path, '->', '->>');
                            END IF;
                            data_path := format('(CASE 
                                WHEN (%s%s) IS NULL THEN NULL
                                WHEN (%s%s) ~ ''^-?[0-9]+\.?[0-9]*$'' THEN (%s%s)::DECIMAL(18,2)
                                ELSE NULL
                            END)', '{}', json_path, '{}', json_path, '{}', json_path);
                            
                            legacy_select_clause := replace(legacy_select_clause,
                                format('NULL::DECIMAL(18,2) AS %I', column_name),
                                format('%s AS %I', replace(data_path, '{}', legacy_source_prefix), column_name)
                            );
                            current_select_clause := replace(current_select_clause,
                                format('NULL::DECIMAL(18,2) AS %I', column_name),
                                format('%s AS %I', replace(data_path, '{}', current_source_prefix), column_name)
                            );
                            
                        WHEN 'option', 'checkbox', 'radio' THEN
                            IF json_path LIKE '%->%' THEN
                                json_path := regexp_replace(json_path, '->([^>]+)$', '->>\1');
                            ELSE
                                json_path := replace(json_path, '->', '->>');
                            END IF;
                            data_path := format('(CASE 
                                WHEN (%s%s) IS NULL THEN NULL
                                WHEN (%s%s) ~ ''^(true|false|t|f|1|0|yes|no)$'' THEN 
                                    CASE 
                                        WHEN lower((%s%s)) IN (''true'', ''t'', ''1'', ''yes'') THEN true
                                        WHEN lower((%s%s)) IN (''false'', ''f'', ''0'', ''no'') THEN false
                                        ELSE NULL
                                    END
                                ELSE NULL
                            END)', '{}', json_path, '{}', json_path, '{}', json_path, '{}', json_path);
                            
                            legacy_select_clause := replace(legacy_select_clause,
                                format('NULL::BOOLEAN AS %I', column_name),
                                format('%s AS %I', replace(data_path, '{}', legacy_source_prefix), column_name)
                            );
                            current_select_clause := replace(current_select_clause,
                                format('NULL::BOOLEAN AS %I', column_name),
                                format('%s AS %I', replace(data_path, '{}', current_source_prefix), column_name)
                            );
                            
                        ELSE
                            -- Default to text
                            IF json_path LIKE '%->%' THEN
                                json_path := regexp_replace(json_path, '->([^>]+)$', '->>\1');
                            ELSE
                                json_path := replace(json_path, '->', '->>');
                            END IF;
                            data_path := format('(%s%s)', '{}', json_path);
                            
                            legacy_select_clause := replace(legacy_select_clause,
                                format('NULL::TEXT AS %I', column_name),
                                format('%s AS %I', replace(data_path, '{}', legacy_source_prefix), column_name)
                            );
                            current_select_clause := replace(current_select_clause,
                                format('NULL::TEXT AS %I', column_name),
                                format('%s AS %I', replace(data_path, '{}', current_source_prefix), column_name)
                            );
                    END CASE;
                END IF;
            END LOOP;
            
            -- Build query for this datagrid and add to array
            datagrid_queries := datagrid_queries || format('(SELECT %s FROM %s WHERE afs."ApplicationFormVersionId" = %L AND afs."Submission"->''submission''->''submission'' IS NULL AND dg_%s_tbl.dg_%s != ''null''::jsonb) UNION ALL (SELECT %s FROM %s WHERE afs."ApplicationFormVersionId" = %L AND afs."Submission"->''submission''->''submission'' IS NOT NULL AND dg_%s_tbl.dg_%s != ''null''::jsonb)',
                legacy_select_clause,
                legacy_from_clause,
                correlation_id,
                datagrid_id,
                datagrid_id,
                current_select_clause,
                current_from_clause,
                correlation_id,
                datagrid_id,
                datagrid_id
            );
        END LOOP;
    END IF;
    
    -- Combine all queries with UNION ALL
    final_query := '';
    
    IF has_root_fields THEN
        final_query := root_query;
    END IF;
    
    IF has_datagrids THEN
        FOR i IN 1..array_length(datagrid_queries, 1) LOOP
            IF final_query != '' THEN
                final_query := final_query || ' UNION ALL ';
            END IF;
            final_query := final_query || datagrid_queries[i];
        END LOOP;
    END IF;
    
    RETURN final_query;
END;
$function$;