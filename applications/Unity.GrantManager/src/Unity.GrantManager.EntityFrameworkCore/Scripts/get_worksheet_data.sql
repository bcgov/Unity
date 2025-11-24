CREATE OR REPLACE FUNCTION "Reporting".get_worksheet_data(correlation_id uuid, report_map_id uuid)
 RETURNS text
 LANGUAGE plpgsql
AS $function$
BEGIN
    RETURN (
        WITH mapping_data AS (
            SELECT 
                row_number() OVER() as mapping_index,
                row_data->>'ColumnName' as column_name,
                lower(row_data->>'Type') as column_type,
                row_data->>'DataPath' as data_path_raw,
                row_data->>'TypePath' as type_path,
                row_data->>'Path' as field_path,
                row_data->>'PropertyName' as property_name,
                row_data->>'Id' as field_id,
                CASE 
                    WHEN row_data->>'DataPath' IS NOT NULL AND row_data->>'DataPath' ~ '^\(' 
                    THEN substring(row_data->>'DataPath' from '^\(([^)]+)\)')
                    ELSE split_part(row_data->>'Path', '->', 1)
                END as worksheet_name,
                CASE 
                    WHEN row_data->>'DataPath' ~ '^\(' 
                    THEN regexp_replace(row_data->>'DataPath', '^\([^)]+\)', '')
                    ELSE row_data->>'DataPath'
                END as clean_data_path
            FROM 
                "Reporting"."ReportColumnsMaps" rcm,
                jsonb_array_elements(rcm."Mapping"->'Rows') as row_data
            WHERE 
                rcm."Id" = report_map_id
                AND (row_data->>'Parent')::boolean IS NOT TRUE
        ),
        -- Use DISTINCT ON to ensure each column gets only one mapping (first occurrence)
        unique_mappings AS (
            SELECT DISTINCT ON (column_name) 
                mapping_index, column_name, column_type, data_path_raw, type_path, 
                field_path, property_name, field_id, worksheet_name, clean_data_path,
                split_part(clean_data_path, '->', 1) as datagrid_name,
                split_part(clean_data_path, '->', 2) as field_name
            FROM mapping_data 
            ORDER BY column_name, mapping_index
        ),
        -- Get UNIQUE worksheet-datagrid combinations for datagrid fields
        unique_worksheet_datagrids AS (
            SELECT DISTINCT worksheet_name, datagrid_name
            FROM unique_mappings 
            WHERE type_path LIKE '%datagrid%'
        ),
        -- Get UNIQUE worksheets that have root-level fields
        unique_worksheets_with_root AS (
            SELECT DISTINCT worksheet_name
            FROM unique_mappings 
            WHERE type_path NOT LIKE '%datagrid%' OR type_path IS NULL
        ),
        -- Build queries for datagrid fields (existing logic)
        datagrid_queries AS (
            SELECT format('
                SELECT 
                    wi."Id" AS worksheet_instance_id,
                    wi."CorrelationId" AS application_id, 
                    COALESCE(w."Name", ''Unknown'') AS worksheet_name,
                    %L || ''_r'' || dg_tbl.row_num AS row_identifier,
                    %s
                FROM "Flex"."WorksheetInstances" wi 
                LEFT JOIN "Flex"."Worksheets" w ON wi."WorksheetId" = w."Id"
                CROSS JOIN LATERAL (
                    SELECT 
                        row_elem as dg_data, 
                        row_number() OVER() as row_num
                    FROM jsonb_array_elements(
                        COALESCE(
                            (SELECT (v_elem->>''value'')::jsonb->''rows'' 
                             FROM jsonb_array_elements(wi."CurrentValue"->''values'') AS v_elem 
                             WHERE v_elem->>''key'' = %L), 
                            ''[null]''::jsonb
                        )
                    ) AS row_elem
                ) AS dg_tbl
                WHERE wi."WorksheetCorrelationId" = %L 
                  AND w."Name" = %L 
                  AND dg_tbl.dg_data != ''null''::jsonb',
                
                uwd.datagrid_name, -- row identifier prefix
                
                -- Build ALL columns with their appropriate field mappings for THIS worksheet-datagrid combination
                (SELECT string_agg(
                    CASE 
                        -- Only populate if this column belongs to this specific worksheet-datagrid
                        WHEN um.worksheet_name = uwd.worksheet_name AND um.datagrid_name = uwd.datagrid_name THEN
                            CASE um.column_type
                                WHEN 'currency' THEN 
                                    format('(CASE WHEN ((SELECT cell_elem->>''value'' FROM jsonb_array_elements(dg_tbl.dg_data->''cells'') AS cell_elem WHERE cell_elem->>''key'' = %L)) IS NULL THEN NULL WHEN ((SELECT cell_elem->>''value'' FROM jsonb_array_elements(dg_tbl.dg_data->''cells'') AS cell_elem WHERE cell_elem->>''key'' = %L)) ~ ''^-?[0-9]+\.?[0-9]*$'' THEN ((SELECT cell_elem->>''value'' FROM jsonb_array_elements(dg_tbl.dg_data->''cells'') AS cell_elem WHERE cell_elem->>''key'' = %L))::DECIMAL(10,2) ELSE NULL END) AS %I',
                                        um.field_name, um.field_name, um.field_name, um.column_name)
                                WHEN 'number' THEN 
                                    format('(CASE WHEN ((SELECT cell_elem->>''value'' FROM jsonb_array_elements(dg_tbl.dg_data->''cells'') AS cell_elem WHERE cell_elem->>''key'' = %L)) IS NULL THEN NULL WHEN ((SELECT cell_elem->>''value'' FROM jsonb_array_elements(dg_tbl.dg_data->''cells'') AS cell_elem WHERE cell_elem->>''key'' = %L)) ~ ''^-?[0-9]+\.?[0-9]*$'' THEN ((SELECT cell_elem->>''value'' FROM jsonb_array_elements(dg_tbl.dg_data->''cells'') AS cell_elem WHERE cell_elem->>''key'' = %L))::NUMERIC ELSE NULL END) AS %I',
                                        um.field_name, um.field_name, um.field_name, um.column_name)
                                WHEN 'date' THEN 
                                    format('(CASE WHEN ((SELECT cell_elem->>''value'' FROM jsonb_array_elements(dg_tbl.dg_data->''cells'') AS cell_elem WHERE cell_elem->>''key'' = %L)) IS NULL OR trim((SELECT cell_elem->>''value'' FROM jsonb_array_elements(dg_tbl.dg_data->''cells'') AS cell_elem WHERE cell_elem->>''key'' = %L)) = '''' THEN NULL ELSE ((SELECT cell_elem->>''value'' FROM jsonb_array_elements(dg_tbl.dg_data->''cells'') AS cell_elem WHERE cell_elem->>''key'' = %L))::TIMESTAMP END) AS %I',
                                        um.field_name, um.field_name, um.field_name, um.column_name)
                                WHEN 'checkbox' THEN 
                                    -- Check if this is a checkbox group field by looking at the type_path
                                    CASE 
                                        WHEN um.type_path LIKE '%checkboxgroup%' THEN
                                            -- For checkbox group, parse the JSON array and extract the specific checkbox value
                                            format('(CASE WHEN ((SELECT cell_elem->>''value'' FROM jsonb_array_elements(dg_tbl.dg_data->''cells'') AS cell_elem WHERE cell_elem->>''key'' = %L)) IS NULL THEN NULL ELSE (SELECT (checkbox_elem->>''value'')::BOOLEAN FROM jsonb_array_elements(((SELECT cell_elem->>''value'' FROM jsonb_array_elements(dg_tbl.dg_data->''cells'') AS cell_elem WHERE cell_elem->>''key'' = %L))::jsonb) AS checkbox_elem WHERE checkbox_elem->>''key'' = %L) END) AS %I',
                                                split_part(um.clean_data_path, '->', 1), -- Field10 equivalent in datagrid
                                                split_part(um.clean_data_path, '->', 1), -- Field10 equivalent in datagrid
                                                split_part(um.clean_data_path, '->', 2), -- check1/check2/etc
                                                um.column_name)
                                        ELSE
                                            -- For regular checkbox, use the existing logic
                                            format('(CASE WHEN ((SELECT cell_elem->>''value'' FROM jsonb_array_elements(dg_tbl.dg_data->''cells'') AS cell_elem WHERE cell_elem->>''key'' = %L)) IS NULL THEN NULL WHEN lower(trim((SELECT cell_elem->>''value'' FROM jsonb_array_elements(dg_tbl.dg_data->''cells'') AS cell_elem WHERE cell_elem->>''key'' = %L))) IN (''true'', ''t'', ''1'', ''yes'', ''on'') THEN TRUE WHEN lower(trim((SELECT cell_elem->>''value'' FROM jsonb_array_elements(dg_tbl.dg_data->''cells'') AS cell_elem WHERE cell_elem->>''key'' = %L))) IN (''false'', ''f'', ''0'', ''no'', ''off'', '''') THEN FALSE ELSE NULL END) AS %I',
                                                um.field_name, um.field_name, um.field_name, um.column_name)
                                    END
                                WHEN 'radio' THEN 
                                    format('((SELECT cell_elem->>''value'' FROM jsonb_array_elements(dg_tbl.dg_data->''cells'') AS cell_elem WHERE cell_elem->>''key'' = %L)) AS %I',
                                        um.field_name, um.column_name)
                                ELSE 
                                    format('((SELECT cell_elem->>''value'' FROM jsonb_array_elements(dg_tbl.dg_data->''cells'') AS cell_elem WHERE cell_elem->>''key'' = %L)) AS %I',
                                        um.field_name, um.column_name)
                            END
                        -- Leave as NULL if this column doesn't belong to this worksheet-datagrid
                        ELSE
                            CASE um.column_type
                                WHEN 'currency' THEN format('NULL::DECIMAL(10,2) AS %I', um.column_name)
                                WHEN 'number' THEN format('NULL::NUMERIC AS %I', um.column_name)
                                WHEN 'date' THEN format('NULL::TIMESTAMP AS %I', um.column_name)
                                WHEN 'checkbox' THEN format('NULL::BOOLEAN AS %I', um.column_name)
                                WHEN 'radio' THEN format('NULL::TEXT AS %I', um.column_name)
                                ELSE format('NULL::TEXT AS %I', um.column_name)
                            END
                    END, ', ' ORDER BY um.column_name
                ) FROM unique_mappings um),
                
                uwd.datagrid_name, -- for COALESCE clause
                correlation_id, -- for WHERE clause
                uwd.worksheet_name -- for WHERE clause
                
            ) as query_text
            FROM unique_worksheet_datagrids uwd
            ORDER BY uwd.worksheet_name, uwd.datagrid_name
        ),
        -- Build queries for root-level fields (NEW)
        root_queries AS (
            SELECT format('
                SELECT 
                    wi."Id" AS worksheet_instance_id,
                    wi."CorrelationId" AS application_id, 
                    COALESCE(w."Name", ''Unknown'') AS worksheet_name,
                    ''root'' AS row_identifier,
                    %s
                FROM "Flex"."WorksheetInstances" wi 
                LEFT JOIN "Flex"."Worksheets" w ON wi."WorksheetId" = w."Id"
                WHERE wi."WorksheetCorrelationId" = %L 
                  AND w."Name" = %L',
                
                -- Build ALL columns with their appropriate field mappings for THIS worksheet's root fields
                (SELECT string_agg(
                    CASE 
                        -- Only populate if this column belongs to this worksheet AND is a root field
                        WHEN um.worksheet_name = uwr.worksheet_name AND (um.type_path NOT LIKE '%datagrid%' OR um.type_path IS NULL) THEN
                            CASE um.column_type
                                WHEN 'currency' THEN 
                                    format('(CASE WHEN ((SELECT v_elem->>''value'' FROM jsonb_array_elements(wi."CurrentValue"->''values'') AS v_elem WHERE v_elem->>''key'' = %L)) IS NULL THEN NULL WHEN ((SELECT v_elem->>''value'' FROM jsonb_array_elements(wi."CurrentValue"->''values'') AS v_elem WHERE v_elem->>''key'' = %L)) ~ ''^-?[0-9]+\.?[0-9]*$'' THEN ((SELECT v_elem->>''value'' FROM jsonb_array_elements(wi."CurrentValue"->''values'') AS v_elem WHERE v_elem->>''key'' = %L))::DECIMAL(10,2) ELSE NULL END) AS %I',
                                        COALESCE(um.clean_data_path, um.property_name), 
                                        COALESCE(um.clean_data_path, um.property_name), 
                                        COALESCE(um.clean_data_path, um.property_name), 
                                        um.column_name)
                                WHEN 'number' THEN 
                                    format('(CASE WHEN ((SELECT v_elem->>''value'' FROM jsonb_array_elements(wi."CurrentValue"->''values'') AS v_elem WHERE v_elem->>''key'' = %L)) IS NULL THEN NULL WHEN ((SELECT v_elem->>''value'' FROM jsonb_array_elements(wi."CurrentValue"->''values'') AS v_elem WHERE v_elem->>''key'' = %L)) ~ ''^-?[0-9]+\.?[0-9]*$'' THEN ((SELECT v_elem->>''value'' FROM jsonb_array_elements(wi."CurrentValue"->''values'') AS v_elem WHERE v_elem->>''key'' = %L))::NUMERIC ELSE NULL END) AS %I',
                                        COALESCE(um.clean_data_path, um.property_name), 
                                        COALESCE(um.clean_data_path, um.property_name), 
                                        COALESCE(um.clean_data_path, um.property_name), 
                                        um.column_name)
                                WHEN 'date' THEN 
                                    format('(CASE WHEN ((SELECT v_elem->>''value'' FROM jsonb_array_elements(wi."CurrentValue"->''values'') AS v_elem WHERE v_elem->>''key'' = %L)) IS NULL OR trim((SELECT v_elem->>''value'' FROM jsonb_array_elements(wi."CurrentValue"->''values'') AS v_elem WHERE v_elem->>''key'' = %L)) = '''' THEN NULL ELSE ((SELECT v_elem->>''value'' FROM jsonb_array_elements(wi."CurrentValue"->''values'') AS v_elem WHERE v_elem->>''key'' = %L))::TIMESTAMP END) AS %I',
                                        COALESCE(um.clean_data_path, um.property_name), 
                                        COALESCE(um.clean_data_path, um.property_name), 
                                        COALESCE(um.clean_data_path, um.property_name), 
                                        um.column_name)
                                WHEN 'checkbox' THEN 
                                    -- Check if this is a checkbox group field by looking at the type_path
                                    CASE 
                                        WHEN um.type_path LIKE '%checkboxgroup%' THEN
                                            -- For checkbox group, parse the JSON array and extract the specific checkbox value
                                            format('(CASE WHEN ((SELECT v_elem->>''value'' FROM jsonb_array_elements(wi."CurrentValue"->''values'') AS v_elem WHERE v_elem->>''key'' = %L)) IS NULL THEN NULL ELSE (SELECT (checkbox_elem->>''value'')::BOOLEAN FROM jsonb_array_elements(((SELECT v_elem->>''value'' FROM jsonb_array_elements(wi."CurrentValue"->''values'') AS v_elem WHERE v_elem->>''key'' = %L))::jsonb) AS checkbox_elem WHERE checkbox_elem->>''key'' = %L) END) AS %I',
                                                split_part(COALESCE(um.clean_data_path, um.property_name), '->', 1), -- Field10
                                                split_part(COALESCE(um.clean_data_path, um.property_name), '->', 1), -- Field10  
                                                split_part(COALESCE(um.clean_data_path, um.property_name), '->', 2), -- check1/check2/etc
                                                um.column_name)
                                        ELSE
                                            -- For regular checkbox, use the existing logic
                                            format('(CASE WHEN ((SELECT v_elem->>''value'' FROM jsonb_array_elements(wi."CurrentValue"->''values'') AS v_elem WHERE v_elem->>''key'' = %L)) IS NULL THEN NULL WHEN lower(trim((SELECT v_elem->>''value'' FROM jsonb_array_elements(wi."CurrentValue"->''values'') AS v_elem WHERE v_elem->>''key'' = %L))) IN (''true'', ''t'', ''1'', ''yes'', ''on'') THEN TRUE WHEN lower(trim((SELECT v_elem->>''value'' FROM jsonb_array_elements(wi."CurrentValue"->''values'') AS v_elem WHERE v_elem->>''key'' = %L))) IN (''false'', ''f'', ''0'', ''no'', ''off'', '''') THEN FALSE ELSE NULL END) AS %I',
                                                COALESCE(um.clean_data_path, um.property_name), 
                                                COALESCE(um.clean_data_path, um.property_name), 
                                                COALESCE(um.clean_data_path, um.property_name), 
                                                um.column_name)
                                    END
                                WHEN 'radio' THEN 
                                    format('((SELECT v_elem->>''value'' FROM jsonb_array_elements(wi."CurrentValue"->''values'') AS v_elem WHERE v_elem->>''key'' = %L)) AS %I',
                                        COALESCE(um.clean_data_path, um.property_name), um.column_name)
                                ELSE 
                                    format('((SELECT v_elem->>''value'' FROM jsonb_array_elements(wi."CurrentValue"->''values'') AS v_elem WHERE v_elem->>''key'' = %L)) AS %I',
                                        COALESCE(um.clean_data_path, um.property_name), um.column_name)
                            END
                        -- Leave as NULL if this column doesn't belong to this worksheet or isn't a root field
                        ELSE
                            CASE um.column_type
                                WHEN 'currency' THEN format('NULL::DECIMAL(10,2) AS %I', um.column_name)
                                WHEN 'number' THEN format('NULL::NUMERIC AS %I', um.column_name)
                                WHEN 'date' THEN format('NULL::TIMESTAMP AS %I', um.column_name)
                                WHEN 'checkbox' THEN format('NULL::BOOLEAN AS %I', um.column_name)
                                WHEN 'radio' THEN format('NULL::TEXT AS %I', um.column_name)
                                ELSE format('NULL::TEXT AS %I', um.column_name)
                            END
                    END, ', ' ORDER BY um.column_name
                ) FROM unique_mappings um),
                
                correlation_id, -- for WHERE clause
                uwr.worksheet_name -- for WHERE clause
                
            ) as query_text
            FROM unique_worksheets_with_root uwr
            ORDER BY uwr.worksheet_name
        ),
        -- Combine all queries
        all_queries AS (
            SELECT query_text FROM datagrid_queries
            UNION ALL
            SELECT query_text FROM root_queries
        )
        SELECT format('
            WITH worksheet_data AS (
                %s
            )
            SELECT * FROM worksheet_data ORDER BY worksheet_name, row_identifier
        ', string_agg(query_text, ' UNION ALL '))
        FROM all_queries
    );
END;
$function$;