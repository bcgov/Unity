
CREATE OR REPLACE FUNCTION "Reporting".get_consolidated_worksheet_data(form_id uuid, report_map_id uuid)
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
                row_data->>'VersionLabel' as version_label,
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
                field_path, property_name, version_label, worksheet_name, clean_data_path,
                split_part(clean_data_path, '->', 1) as datagrid_name,
                split_part(clean_data_path, '->', 2) as field_name
            FROM mapping_data
            ORDER BY column_name, mapping_index
        ),
        -- Load form version IDs from the consolidated mapping metadata
        -- Keys are like "formversion_{versionId}" with value "v1", "v2", etc.
        version_info AS (
            SELECT
                replace(kv.key, 'formversion_', '')::uuid as version_id,
                kv.value as version_label
            FROM "Reporting"."ReportColumnsMaps" rcm,
            jsonb_each_text(rcm."Mapping"->'Metadata'->'Info') kv
            WHERE rcm."Id" = report_map_id
              AND kv.key LIKE 'formversion_%'
        ),
        -- Get unique worksheet names from the mapping data
        unique_worksheet_names AS (
            SELECT DISTINCT worksheet_name
            FROM unique_mappings
            WHERE worksheet_name IS NOT NULL AND worksheet_name <> ''
        ),
        -- Get unique datagrid combinations (worksheet_name, datagrid_name) from mapping
        unique_datagrid_combinations AS (
            SELECT DISTINCT worksheet_name, datagrid_name
            FROM unique_mappings
            WHERE type_path LIKE '%datagrid%'
              AND datagrid_name IS NOT NULL AND datagrid_name <> ''
        ),
        -- Build datagrid queries: one per (version, worksheet, datagrid) combination
        datagrid_queries AS (
            SELECT format('
                SELECT
                    wi."Id" AS worksheet_instance_id,
                    wi."CorrelationId" AS application_id,
                    COALESCE(w."Name", ''Unknown'') AS worksheet_name,
                    %L || ''_r'' || dg_tbl.row_num AS row_identifier,
                    %L AS form_version_label,
                    %s
                FROM "Flex"."WorksheetInstances" wi
                LEFT JOIN "Flex"."Worksheets" w ON wi."WorksheetId" = w."Id"
                CROSS JOIN LATERAL (
                    SELECT
                        row_elem as dg_data,
                        row_number() OVER() as row_num
                    FROM jsonb_array_elements(
                        COALESCE(
                            (SELECT "Reporting".safe_to_jsonb(v_elem->>''value'')->''rows''
                             FROM jsonb_array_elements(wi."CurrentValue"->''values'') AS v_elem
                             WHERE v_elem->>''key'' = %L),
                             ''[null]''::jsonb
                        )
                    ) AS row_elem
                ) AS dg_tbl
                WHERE wi."WorksheetCorrelationId" = %L
                  AND w."Name" = %L
                  AND dg_tbl.dg_data != ''null''::jsonb',

                udc.datagrid_name,   -- row identifier prefix
                vi.version_label,    -- form_version_label literal

                -- Build ALL columns with type-appropriate extraction
                (SELECT string_agg(
                    CASE
                        -- This column belongs to this worksheet-datagrid AND this version (or is merged)
                        WHEN um.worksheet_name = udc.worksheet_name
                          AND um.datagrid_name = udc.datagrid_name
                          AND (um.version_label IS NULL OR um.version_label = vi.version_label) THEN
                            CASE um.column_type
                                WHEN 'currency' THEN
                                    format('(CASE WHEN ((SELECT cell_elem->>''value'' FROM jsonb_array_elements(dg_tbl.dg_data->''cells'') AS cell_elem WHERE cell_elem->>''key'' = %L)) IS NULL THEN NULL WHEN replace(btrim((SELECT cell_elem->>''value'' FROM jsonb_array_elements(dg_tbl.dg_data->''cells'') AS cell_elem WHERE cell_elem->>''key'' = %L)), '','', '''') ~ ''^-?[0-9]+\.?[0-9]*$'' THEN replace(btrim((SELECT cell_elem->>''value'' FROM jsonb_array_elements(dg_tbl.dg_data->''cells'') AS cell_elem WHERE cell_elem->>''key'' = %L)), '','', '''')::DECIMAL(18,2) ELSE NULL END) AS %I',
                                        um.field_name, um.field_name, um.field_name, um.column_name)
                                WHEN 'number' THEN
                                    format('(CASE WHEN ((SELECT cell_elem->>''value'' FROM jsonb_array_elements(dg_tbl.dg_data->''cells'') AS cell_elem WHERE cell_elem->>''key'' = %L)) IS NULL THEN NULL WHEN ((SELECT cell_elem->>''value'' FROM jsonb_array_elements(dg_tbl.dg_data->''cells'') AS cell_elem WHERE cell_elem->>''key'' = %L)) ~ ''^-?[0-9]+\.?[0-9]*$'' THEN ((SELECT cell_elem->>''value'' FROM jsonb_array_elements(dg_tbl.dg_data->''cells'') AS cell_elem WHERE cell_elem->>''key'' = %L))::NUMERIC ELSE NULL END) AS %I',
                                        um.field_name, um.field_name, um.field_name, um.column_name)
                                WHEN 'numeric' THEN
                                    format('(CASE WHEN ((SELECT cell_elem->>''value'' FROM jsonb_array_elements(dg_tbl.dg_data->''cells'') AS cell_elem WHERE cell_elem->>''key'' = %L)) IS NULL THEN NULL WHEN ((SELECT cell_elem->>''value'' FROM jsonb_array_elements(dg_tbl.dg_data->''cells'') AS cell_elem WHERE cell_elem->>''key'' = %L)) ~ ''^-?[0-9]+\.?[0-9]*$'' THEN ((SELECT cell_elem->>''value'' FROM jsonb_array_elements(dg_tbl.dg_data->''cells'') AS cell_elem WHERE cell_elem->>''key'' = %L))::NUMERIC ELSE NULL END) AS %I',
                                        um.field_name, um.field_name, um.field_name, um.column_name)
                                WHEN 'date' THEN
                                    format('"Reporting".safe_to_date((SELECT cell_elem->>''value'' FROM jsonb_array_elements(dg_tbl.dg_data->''cells'') AS cell_elem WHERE cell_elem->>''key'' = %L)) AS %I',
                                        um.field_name, um.column_name)
                                WHEN 'datetime' THEN
                                    format('"Reporting".safe_to_timestamp((SELECT cell_elem->>''value'' FROM jsonb_array_elements(dg_tbl.dg_data->''cells'') AS cell_elem WHERE cell_elem->>''key'' = %L)) AS %I',
                                        um.field_name, um.column_name)
                                WHEN 'checkbox' THEN
                                    CASE
                                        WHEN um.type_path LIKE '%checkboxgroup%' THEN
                                            format('(CASE WHEN ((SELECT cell_elem->>''value'' FROM jsonb_array_elements(dg_tbl.dg_data->''cells'') AS cell_elem WHERE cell_elem->>''key'' = %L)) IS NULL THEN NULL ELSE (SELECT (checkbox_elem->>''value'')::BOOLEAN FROM jsonb_array_elements(((SELECT cell_elem->>''value'' FROM jsonb_array_elements(dg_tbl.dg_data->''cells'') AS cell_elem WHERE cell_elem->>''key'' = %L))::jsonb) AS checkbox_elem WHERE checkbox_elem->>''key'' = %L) END) AS %I',
                                                split_part(um.clean_data_path, '->', 1),
                                                split_part(um.clean_data_path, '->', 1),
                                                split_part(um.clean_data_path, '->', 2),
                                                um.column_name)
                                        ELSE
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
                        -- Column doesn't apply to this version/worksheet-datagrid — emit typed NULL
                        ELSE
                            CASE um.column_type
                                WHEN 'currency' THEN format('NULL::DECIMAL(18,2) AS %I', um.column_name)
                                WHEN 'number' THEN format('NULL::NUMERIC AS %I', um.column_name)
                                WHEN 'numeric' THEN format('NULL::NUMERIC AS %I', um.column_name)
                                WHEN 'date' THEN format('NULL::DATE AS %I', um.column_name)
                                WHEN 'datetime' THEN format('NULL::TIMESTAMP AS %I', um.column_name)
                                WHEN 'checkbox' THEN format('NULL::BOOLEAN AS %I', um.column_name)
                                WHEN 'radio' THEN format('NULL::TEXT AS %I', um.column_name)
                                ELSE format('NULL::TEXT AS %I', um.column_name)
                            END
                    END, ', ' ORDER BY um.column_name
                ) FROM unique_mappings um),

                udc.datagrid_name,    -- for COALESCE clause
                vi.version_id,        -- for WHERE WorksheetCorrelationId
                udc.worksheet_name    -- for WHERE worksheet Name

            ) as query_text
            FROM unique_datagrid_combinations udc
            CROSS JOIN version_info vi
            ORDER BY vi.version_label, udc.worksheet_name, udc.datagrid_name
        ),
        -- Build root-level (non-datagrid) queries: one per (version, worksheet) combination
        root_queries AS (
            SELECT format('
                SELECT
                    wi."Id" AS worksheet_instance_id,
                    wi."CorrelationId" AS application_id,
                    COALESCE(w."Name", ''Unknown'') AS worksheet_name,
                    ''root'' AS row_identifier,
                    %L AS form_version_label,
                    %s
                FROM "Flex"."WorksheetInstances" wi
                LEFT JOIN "Flex"."Worksheets" w ON wi."WorksheetId" = w."Id"
                WHERE wi."WorksheetCorrelationId" = %L
                  AND w."Name" = %L',

                vi.version_label,    -- form_version_label literal

                -- Build ALL columns with type-appropriate extraction
                (SELECT string_agg(
                    CASE
                        -- This column belongs to this worksheet root AND this version (or is merged)
                        WHEN um.worksheet_name = uwn.worksheet_name
                          AND (um.type_path NOT LIKE '%datagrid%' OR um.type_path IS NULL)
                          AND (um.version_label IS NULL OR um.version_label = vi.version_label) THEN
                            CASE um.column_type
                                WHEN 'currency' THEN
                                    format('(CASE WHEN ((SELECT v_elem->>''value'' FROM jsonb_array_elements(wi."CurrentValue"->''values'') AS v_elem WHERE v_elem->>''key'' = %L)) IS NULL THEN NULL WHEN replace(btrim((SELECT v_elem->>''value'' FROM jsonb_array_elements(wi."CurrentValue"->''values'') AS v_elem WHERE v_elem->>''key'' = %L)), '','', '''') ~ ''^-?[0-9]+\.?[0-9]*$'' THEN replace(btrim((SELECT v_elem->>''value'' FROM jsonb_array_elements(wi."CurrentValue"->''values'') AS v_elem WHERE v_elem->>''key'' = %L)), '','', '''')::DECIMAL(18,2) ELSE NULL END) AS %I',
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
                                WHEN 'numeric' THEN
                                    format('(CASE WHEN ((SELECT v_elem->>''value'' FROM jsonb_array_elements(wi."CurrentValue"->''values'') AS v_elem WHERE v_elem->>''key'' = %L)) IS NULL THEN NULL WHEN ((SELECT v_elem->>''value'' FROM jsonb_array_elements(wi."CurrentValue"->''values'') AS v_elem WHERE v_elem->>''key'' = %L)) ~ ''^-?[0-9]+\.?[0-9]*$'' THEN ((SELECT v_elem->>''value'' FROM jsonb_array_elements(wi."CurrentValue"->''values'') AS v_elem WHERE v_elem->>''key'' = %L))::NUMERIC ELSE NULL END) AS %I',
                                        COALESCE(um.clean_data_path, um.property_name),
                                        COALESCE(um.clean_data_path, um.property_name),
                                        COALESCE(um.clean_data_path, um.property_name),
                                        um.column_name)
                                WHEN 'date' THEN
                                    format('"Reporting".safe_to_date((SELECT v_elem->>''value'' FROM jsonb_array_elements(wi."CurrentValue"->''values'') AS v_elem WHERE v_elem->>''key'' = %L)) AS %I',
                                        COALESCE(um.clean_data_path, um.property_name),
                                        um.column_name)
                                WHEN 'datetime' THEN
                                    format('"Reporting".safe_to_timestamp((SELECT v_elem->>''value'' FROM jsonb_array_elements(wi."CurrentValue"->''values'') AS v_elem WHERE v_elem->>''key'' = %L)) AS %I',
                                        COALESCE(um.clean_data_path, um.property_name),
                                        um.column_name)
                                WHEN 'checkbox' THEN
                                    CASE
                                        WHEN um.type_path LIKE '%checkboxgroup%' THEN
                                            format('(CASE WHEN ((SELECT v_elem->>''value'' FROM jsonb_array_elements(wi."CurrentValue"->''values'') AS v_elem WHERE v_elem->>''key'' = %L)) IS NULL THEN NULL ELSE (SELECT (checkbox_elem->>''value'')::BOOLEAN FROM jsonb_array_elements(((SELECT v_elem->>''value'' FROM jsonb_array_elements(wi."CurrentValue"->''values'') AS v_elem WHERE v_elem->>''key'' = %L))::jsonb) AS checkbox_elem WHERE checkbox_elem->>''key'' = %L) END) AS %I',
                                                split_part(COALESCE(um.clean_data_path, um.property_name), '->', 1),
                                                split_part(COALESCE(um.clean_data_path, um.property_name), '->', 1),
                                                split_part(COALESCE(um.clean_data_path, um.property_name), '->', 2),
                                                um.column_name)
                                        ELSE
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
                        -- Column doesn't apply to this version/worksheet root — emit typed NULL
                        ELSE
                            CASE um.column_type
                                WHEN 'currency' THEN format('NULL::DECIMAL(18,2) AS %I', um.column_name)
                                WHEN 'number' THEN format('NULL::NUMERIC AS %I', um.column_name)
                                WHEN 'numeric' THEN format('NULL::NUMERIC AS %I', um.column_name)
                                WHEN 'date' THEN format('NULL::DATE AS %I', um.column_name)
                                WHEN 'datetime' THEN format('NULL::TIMESTAMP AS %I', um.column_name)
                                WHEN 'checkbox' THEN format('NULL::BOOLEAN AS %I', um.column_name)
                                WHEN 'radio' THEN format('NULL::TEXT AS %I', um.column_name)
                                ELSE format('NULL::TEXT AS %I', um.column_name)
                            END
                    END, ', ' ORDER BY um.column_name
                ) FROM unique_mappings um),

                vi.version_id,        -- for WHERE WorksheetCorrelationId
                uwn.worksheet_name    -- for WHERE worksheet Name

            ) as query_text
            FROM unique_worksheet_names uwn
            CROSS JOIN version_info vi
            ORDER BY vi.version_label, uwn.worksheet_name
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
            SELECT * FROM worksheet_data ORDER BY form_version_label, worksheet_name, row_identifier
        ', string_agg(query_text, ' UNION ALL '))
        FROM all_queries
    );
END;
$function$;
