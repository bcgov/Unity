CREATE OR REPLACE FUNCTION "Reporting".get_scoresheet_data(correlation_id uuid, report_map_id uuid)
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
    key_name TEXT;
    base_select_clause TEXT;
    current_select_clause TEXT;
    current_from_clause TEXT;
    data_path TEXT;
    json_path TEXT;
    path_parts TEXT[];
    all_columns JSONB := '{}';
    column_type_conflicts JSONB := '{}';
    current_source_prefix TEXT;
    final_query TEXT;
    column_list TEXT;
    use_text_fallback BOOLEAN := false;
    i INTEGER;
    normalized_type TEXT;
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
        column_type := lower(row_data->>'Type'); -- Normalize to lowercase for comparison
        type_path := row_data->>'TypePath';
        data_path_raw := row_data->>'DataPath';
        
        -- Check for type conflicts with existing columns
        IF all_columns ? column_name THEN
            -- Column already exists, check if types match
            IF (all_columns->column_name->>'type') != column_type THEN
                -- Type conflict detected - mark for TEXT fallback
                column_type_conflicts := column_type_conflicts || jsonb_build_object(column_name, true);
                use_text_fallback := true;
            END IF;
        END IF;
        
        -- Store column info with normalized type
        all_columns := all_columns || jsonb_build_object(column_name, jsonb_build_object(
            'type', column_type,
            'type_path', COALESCE(type_path, ''),
            'data_path', COALESCE(data_path_raw, '')
        ));
    END LOOP;
    
    -- Build standardized column list with appropriate types for scoresheet question types only
    column_list := '';
    FOR column_name IN SELECT jsonb_object_keys(all_columns) ORDER BY jsonb_object_keys(all_columns) LOOP
        IF column_list != '' THEN
            column_list := column_list || ', ';
        END IF;
        
        -- Use TEXT only if there's a type conflict for this column
        IF use_text_fallback AND (column_type_conflicts ? column_name) THEN
            column_list := column_list || format('NULL::TEXT AS %I', column_name);
        ELSE
            -- Use the original type for the column - only handle actual scoresheet question types
            column_type := all_columns->column_name->>'type';
            CASE column_type
                WHEN 'number' THEN
                    column_list := column_list || format('NULL::NUMERIC AS %I', column_name);
                WHEN 'yesno' THEN
                    column_list := column_list || format('NULL::BOOLEAN AS %I', column_name);
                ELSE
                    -- Default for 'text', 'textarea', 'selectlist' and any other types
                    column_list := column_list || format('NULL::TEXT AS %I', column_name);
            END CASE;
        END IF;
    END LOOP;
    
    -- Initialize base select clause with common fields including application_id
    -- correlation_id is the application_form_id
    base_select_clause := 'si."Id" AS scoresheet_instance_id, ap."Id" AS application_id, si."CorrelationId" AS assessment_id, si."ScoresheetId" AS scoresheet_id, "Reporting".calculate_scoresheet_total_score(si."Id") AS total_score';
    
    -- Add all columns as placeholders first
    IF column_list != '' THEN
        base_select_clause := base_select_clause || ', ' || column_list;
    END IF;
    
    -- Initialize FROM clause with proper JOINs
    current_from_clause := '"Flex"."ScoresheetInstances" si
        JOIN "Assessments" a ON si."CorrelationId" = a."Id"
        JOIN "Applications" ap ON a."ApplicationId" = ap."Id"';
    
    -- Initialize select clause
    current_select_clause := base_select_clause;
    
    -- Process fields - extract values exclusively from Answers table CurrentValue JSON
    FOR i IN 0..(jsonb_array_length(mapping_rows) - 1) LOOP
        row_data := mapping_rows->i;
        
        -- Skip parent rows
        IF (row_data->>'Parent')::boolean = true THEN
            CONTINUE;
        END IF;
        
        column_name := row_data->>'ColumnName';
        column_type := lower(row_data->>'Type'); -- Normalize to lowercase for comparison
        property_name := row_data->>'PropertyName';
        data_path_raw := row_data->>'DataPath';
        key_name := row_data->>'Key';
        
        -- Extract from Answers table CurrentValue JSON only
        -- Use the Key, PropertyName, or DataPath to match against Question.Name
        IF key_name IS NOT NULL AND trim(key_name) != '' THEN
            current_source_prefix := format('(
                SELECT a_1."CurrentValue"->>''value''
                FROM "Flex"."Answers" a_1
                JOIN "Flex"."Questions" q ON a_1."QuestionId" = q."Id"
                WHERE a_1."ScoresheetInstanceId" = si."Id" 
                AND q."Name" = ''%s''
                LIMIT 1
            )', key_name);
        ELSIF property_name IS NOT NULL AND trim(property_name) != '' THEN
            current_source_prefix := format('(
                SELECT a_1."CurrentValue"->>''value''
                FROM "Flex"."Answers" a_1
                JOIN "Flex"."Questions" q ON a_1."QuestionId" = q."Id"
                WHERE a_1."ScoresheetInstanceId" = si."Id" 
                AND q."Name" = ''%s''
                LIMIT 1
            )', property_name);
        ELSIF data_path_raw IS NOT NULL AND trim(data_path_raw) != '' THEN
            current_source_prefix := format('(
                SELECT a_1."CurrentValue"->>''value''
                FROM "Flex"."Answers" a_1
                JOIN "Flex"."Questions" q ON a_1."QuestionId" = q."Id"
                WHERE a_1."ScoresheetInstanceId" = si."Id" 
                AND q."Name" = ''%s''
                LIMIT 1
            )', data_path_raw);
        ELSE
            -- Fallback to column name
            current_source_prefix := format('(
                SELECT a_1."CurrentValue"->>''value''
                FROM "Flex"."Answers" a_1
                JOIN "Flex"."Questions" q ON a_1."QuestionId" = q."Id"
                WHERE a_1."ScoresheetInstanceId" = si."Id" 
                AND q."Name" = ''%s''
                LIMIT 1
            )', column_name);
        END IF;
        
        -- Apply type-specific extraction - only handle actual scoresheet question types
        IF use_text_fallback AND (column_type_conflicts ? column_name) THEN
            -- Force TEXT for conflicted columns
            data_path := format('(%s)::TEXT', current_source_prefix);
        ELSE
            -- Use original types - only handle actual scoresheet QuestionType enum values
            CASE column_type
                WHEN 'number' THEN
                    data_path := format('(CASE 
                        WHEN (%s) IS NULL THEN NULL
                        WHEN (%s) ~ ''^-?[0-9]+\.?[0-9]*$'' THEN (%s)::NUMERIC
                        ELSE NULL
                    END)', current_source_prefix, current_source_prefix, current_source_prefix);
                    
                WHEN 'yesno' THEN
                    data_path := format('(CASE 
                        WHEN (%s) IS NULL THEN NULL
                        WHEN lower((%s)) IN (''true'', ''t'', ''1'', ''yes'') THEN true
                        WHEN lower((%s)) IN (''false'', ''f'', ''0'', ''no'') THEN false
                        ELSE NULL
                    END)', current_source_prefix, current_source_prefix, current_source_prefix);
                    
                ELSE
                    -- Default for 'text', 'textarea', 'selectlist' and any other types
                    data_path := format('%s', current_source_prefix);
            END CASE;
        END IF;
        
        -- Replace the NULL placeholder with the actual value
        IF use_text_fallback AND (column_type_conflicts ? column_name) THEN
            current_select_clause := replace(current_select_clause,
                format('NULL::TEXT AS %I', column_name),
                format('%s AS %I', data_path, column_name)
            );
        ELSE
            -- Replace with proper typed placeholder - only handle actual scoresheet question types
            CASE column_type
                WHEN 'number' THEN
                    current_select_clause := replace(current_select_clause,
                        format('NULL::NUMERIC AS %I', column_name),
                        format('%s AS %I', data_path, column_name)
                    );
                WHEN 'yesno' THEN
                    current_select_clause := replace(current_select_clause,
                        format('NULL::BOOLEAN AS %I', column_name),
                        format('%s AS %I', data_path, column_name)
                    );
                ELSE
                    -- Default for 'text', 'textarea', 'selectlist' and any other types
                    current_select_clause := replace(current_select_clause,
                        format('NULL::TEXT AS %I', column_name),
                        format('%s AS %I', data_path, column_name)
                    );
            END CASE;
        END IF;
    END LOOP;
    
    -- Build final query filtering by application form ID (correlation_id)
    final_query := format('SELECT %s FROM %s 
        WHERE ap."ApplicationFormId" = %L',
        current_select_clause,
        current_from_clause,
        correlation_id
    );
    
    RETURN final_query;
END;
$function$;