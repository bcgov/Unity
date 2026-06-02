CREATE OR REPLACE FUNCTION "Reporting".get_consolidated_formversion_data(form_id uuid, report_map_id uuid)
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
    version_id UUID;
    version_lbl TEXT;
    all_columns JSONB := '{}';
    unique_datagrids JSONB := '{}';
    has_root_fields BOOLEAN := false;
    has_datagrids BOOLEAN := false;
    column_list TEXT;
    base_select_clause TEXT;
    legacy_select_clause TEXT;
    current_select_clause TEXT;
    legacy_from_clause TEXT;
    current_from_clause TEXT;
    json_path TEXT;
    path_parts TEXT[];
    field_name TEXT;
    data_path TEXT;
    legacy_source_prefix TEXT;
    current_source_prefix TEXT;
    all_queries TEXT[];
    i INTEGER;
    j INTEGER;
BEGIN
    -- Fetch the mapping data for this report map ID
    SELECT "Mapping"->'Rows' INTO mapping_rows
    FROM "Reporting"."ReportColumnsMaps"
    WHERE "Id" = report_map_id;

    IF mapping_rows IS NULL OR jsonb_array_length(mapping_rows) = 0 THEN
        RAISE EXCEPTION 'No mapping rows found for consolidated formversion report map: %', report_map_id;
    END IF;

    -- First pass: collect all unique column names and detect datagrids
    FOR i IN 0..(jsonb_array_length(mapping_rows) - 1) LOOP
        row_data := mapping_rows->i;
        IF (row_data->>'Parent')::boolean = true THEN CONTINUE; END IF;

        column_name := row_data->>'ColumnName';
        column_type := lower(COALESCE(row_data->>'Type', 'text'));
        type_path := row_data->>'TypePath';
        data_path_raw := row_data->>'DataPath';
        datagrid_id := row_data->>'Id';

        -- Only store first occurrence per column_name
        IF NOT (all_columns ? column_name) THEN
            all_columns := all_columns || jsonb_build_object(column_name, jsonb_build_object(
                'type', column_type,
                'type_path', COALESCE(type_path, ''),
                'data_path', COALESCE(data_path_raw, '')
            ));
        END IF;

        IF type_path IS NOT NULL AND type_path LIKE '%datagrid%' THEN
            has_datagrids := true;
            IF data_path_raw IS NOT NULL THEN
                IF data_path_raw ~ '^\(DK[0-9]+\)' THEN
                    datagrid_name := split_part(regexp_replace(data_path_raw, '^\(DK[0-9]+\)', ''), '->', 1);
                ELSE
                    datagrid_name := split_part(data_path_raw, '->', 1);
                END IF;
                IF datagrid_name IS NOT NULL AND datagrid_name != '' AND NOT (unique_datagrids ? datagrid_name) THEN
                    unique_datagrids := unique_datagrids || jsonb_build_object(datagrid_name, datagrid_id);
                END IF;
            END IF;
        ELSE
            has_root_fields := true;
        END IF;
    END LOOP;

    -- Build typed NULL column list (ordered by column name for consistent UNION ALL column order)
    column_list := '';
    FOR column_name IN SELECT jsonb_object_keys(all_columns) ORDER BY jsonb_object_keys(all_columns) LOOP
        IF column_list != '' THEN column_list := column_list || ', '; END IF;
        column_type := all_columns->column_name->>'type';
        CASE column_type
            WHEN 'number' THEN column_list := column_list || format('NULL::NUMERIC AS %I', column_name);
            WHEN 'currency' THEN column_list := column_list || format('NULL::DECIMAL(18,2) AS %I', column_name);
            WHEN 'option', 'checkbox' THEN column_list := column_list || format('NULL::BOOLEAN AS %I', column_name);
            ELSE column_list := column_list || format('NULL::TEXT AS %I', column_name);
        END CASE;
    END LOOP;

    all_queries := '{}';

    -- Loop over each form version from metadata (ordered by version label)
    FOR version_id, version_lbl IN
        SELECT replace(kv.key, 'formversion_', '')::uuid, kv.value
        FROM "Reporting"."ReportColumnsMaps" rcm,
        jsonb_each_text(rcm."Mapping"->'Metadata'->'Info') kv
        WHERE rcm."Id" = report_map_id
          AND kv.key LIKE 'formversion_%'
        ORDER BY kv.value
    LOOP

        -- ===== ROOT QUERY for this version =====
        IF has_root_fields THEN
            base_select_clause := format(
                'afs."Id" AS submission_id, afs."ApplicationId" AS application_id, ''root'' AS row_identifier, %L AS form_version_label',
                version_lbl
            );
            IF column_list != '' THEN
                base_select_clause := base_select_clause || ', ' || column_list;
            END IF;

            legacy_from_clause := 'public."ApplicationFormSubmissions" afs';
            current_from_clause := 'public."ApplicationFormSubmissions" afs';
            legacy_select_clause := base_select_clause;
            current_select_clause := base_select_clause;

            FOR i IN 0..(jsonb_array_length(mapping_rows) - 1) LOOP
                row_data := mapping_rows->i;
                IF (row_data->>'Parent')::boolean = true THEN CONTINUE; END IF;
                IF (row_data->>'TypePath') IS NOT NULL AND (row_data->>'TypePath') LIKE '%datagrid%' THEN CONTINUE; END IF;

                column_name := row_data->>'ColumnName';
                column_type := lower(COALESCE(row_data->>'Type', 'text'));
                property_name := row_data->>'PropertyName';
                data_path_raw := row_data->>'DataPath';

                -- Version gating: skip columns that don't include this version
                -- VersionLabel is null (All), a single version ("v1"), or comma-separated ("v1, v2")
                IF (row_data->>'VersionLabel') IS NOT NULL
                   AND NOT (version_lbl = ANY(string_to_array(row_data->>'VersionLabel', ', '))) THEN
                    CONTINUE; -- Leave as typed NULL placeholder
                END IF;

                -- Build json_path from DataPath or PropertyName
                IF data_path_raw IS NOT NULL AND trim(data_path_raw) != '' THEN
                    path_parts := string_to_array(data_path_raw, '->');
                    json_path := '';
                    FOR j IN 1..array_length(path_parts, 1) LOOP
                        IF trim(path_parts[j]) = '' THEN CONTINUE; END IF;
                        IF json_path != '' THEN json_path := json_path || '->'; END IF;
                        json_path := json_path || format('''%s''', trim(path_parts[j]));
                    END LOOP;
                    json_path := '->' || json_path;
                ELSE
                    json_path := format('->''%s''', property_name);
                END IF;

                legacy_source_prefix := 'afs."Submission"->''submission''->''data''';
                current_source_prefix := 'afs."Submission"->''submission''->''submission''->''data''';

                CASE column_type
                    WHEN 'textfield', 'textarea', 'email', 'select', 'phonenumber' THEN
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
                        data_path := format('(CASE WHEN (%s%s) IS NULL THEN NULL WHEN (%s%s) ~ ''^-?[0-9]+\.?[0-9]*$'' THEN (%s%s)::NUMERIC ELSE NULL END)', '{}', json_path, '{}', json_path, '{}', json_path);
                    WHEN 'currency' THEN
                        IF json_path LIKE '%->%' THEN
                            json_path := regexp_replace(json_path, '->([^>]+)$', '->>\1');
                        ELSE
                            json_path := replace(json_path, '->', '->>');
                        END IF;
                        data_path := format('(CASE WHEN (%s%s) IS NULL THEN NULL WHEN (%s%s) ~ ''^-?[0-9]+\.?[0-9]*$'' THEN (%s%s)::DECIMAL(18,2) ELSE NULL END)', '{}', json_path, '{}', json_path, '{}', json_path);
                    WHEN 'option', 'checkbox' THEN
                        IF json_path LIKE '%->%' THEN
                            json_path := regexp_replace(json_path, '->([^>]+)$', '->>\1');
                        ELSE
                            json_path := replace(json_path, '->', '->>');
                        END IF;
                        data_path := format('(CASE WHEN (%s%s) IS NULL THEN NULL WHEN (%s%s) ~ ''^(true|false|t|f|1|0|yes|no)$'' THEN CASE WHEN lower((%s%s)) IN (''true'', ''t'', ''1'', ''yes'') THEN true WHEN lower((%s%s)) IN (''false'', ''f'', ''0'', ''no'') THEN false ELSE NULL END ELSE NULL END)',
                            '{}', json_path, '{}', json_path, '{}', json_path, '{}', json_path);
                    ELSE
                        IF json_path LIKE '%->%' THEN
                            json_path := regexp_replace(json_path, '->([^>]+)$', '->>\1');
                        ELSE
                            json_path := replace(json_path, '->', '->>');
                        END IF;
                        data_path := format('(%s%s)', '{}', json_path);
                END CASE;

                -- Replace typed NULL placeholder with actual extraction
                CASE column_type
                    WHEN 'number' THEN
                        legacy_select_clause := regexp_replace(legacy_select_clause,
                            format('NULL::NUMERIC AS %I([^_A-Za-z0-9]|$)', column_name),
                            format('%s AS %I\1', replace(data_path, '{}', legacy_source_prefix), column_name));
                        current_select_clause := regexp_replace(current_select_clause,
                            format('NULL::NUMERIC AS %I([^_A-Za-z0-9]|$)', column_name),
                            format('%s AS %I\1', replace(data_path, '{}', current_source_prefix), column_name));
                    WHEN 'currency' THEN
                        legacy_select_clause := regexp_replace(legacy_select_clause,
                            format('NULL::DECIMAL\(18,2\) AS %I([^_A-Za-z0-9]|$)', column_name),
                            format('%s AS %I\1', replace(data_path, '{}', legacy_source_prefix), column_name));
                        current_select_clause := regexp_replace(current_select_clause,
                            format('NULL::DECIMAL\(18,2\) AS %I([^_A-Za-z0-9]|$)', column_name),
                            format('%s AS %I\1', replace(data_path, '{}', current_source_prefix), column_name));
                    WHEN 'option', 'checkbox' THEN
                        legacy_select_clause := regexp_replace(legacy_select_clause,
                            format('NULL::BOOLEAN AS %I([^_A-Za-z0-9]|$)', column_name),
                            format('%s AS %I\1', replace(data_path, '{}', legacy_source_prefix), column_name));
                        current_select_clause := regexp_replace(current_select_clause,
                            format('NULL::BOOLEAN AS %I([^_A-Za-z0-9]|$)', column_name),
                            format('%s AS %I\1', replace(data_path, '{}', current_source_prefix), column_name));
                    ELSE
                        legacy_select_clause := regexp_replace(legacy_select_clause,
                            format('NULL::TEXT AS %I([^_A-Za-z0-9]|$)', column_name),
                            format('%s AS %I\1', replace(data_path, '{}', legacy_source_prefix), column_name));
                        current_select_clause := regexp_replace(current_select_clause,
                            format('NULL::TEXT AS %I([^_A-Za-z0-9]|$)', column_name),
                            format('%s AS %I\1', replace(data_path, '{}', current_source_prefix), column_name));
                END CASE;
            END LOOP;

            all_queries := all_queries || format(
                '(SELECT %s FROM %s WHERE afs."ApplicationFormVersionId" = %L AND afs."Submission"->''submission''->''submission'' IS NULL) UNION ALL (SELECT %s FROM %s WHERE afs."ApplicationFormVersionId" = %L AND afs."Submission"->''submission''->''submission'' IS NOT NULL)',
                legacy_select_clause, legacy_from_clause, version_id,
                current_select_clause, current_from_clause, version_id
            );
        END IF;

        -- ===== DATAGRID QUERIES for this version =====
        IF has_datagrids THEN
            FOR datagrid_name IN SELECT jsonb_object_keys(unique_datagrids) LOOP
                datagrid_id := unique_datagrids->>datagrid_name;

                base_select_clause := format(
                    'afs."Id" AS submission_id, afs."ApplicationId" AS application_id, %L || ''_r'' || dg_%s_tbl.dg_%s_row_num AS row_identifier, %L AS form_version_label',
                    datagrid_name, datagrid_id, datagrid_id, version_lbl
                );
                IF column_list != '' THEN
                    base_select_clause := base_select_clause || ', ' || column_list;
                END IF;

                legacy_from_clause := format(
                    'public."ApplicationFormSubmissions" afs CROSS JOIN LATERAL (
                        SELECT elem AS dg_%s, row_number() OVER() AS dg_%s_row_num
                        FROM jsonb_array_elements(COALESCE(afs."Submission"->''submission''->''data''->''%s'', ''[null]''::jsonb)) AS elem
                    ) AS dg_%s_tbl',
                    datagrid_id, datagrid_id, datagrid_name, datagrid_id
                );

                current_from_clause := format(
                    'public."ApplicationFormSubmissions" afs CROSS JOIN LATERAL (
                        SELECT elem AS dg_%s, row_number() OVER() AS dg_%s_row_num
                        FROM jsonb_array_elements(COALESCE(afs."Submission"->''submission''->''submission''->''data''->''%s'', ''[null]''::jsonb)) AS elem
                    ) AS dg_%s_tbl',
                    datagrid_id, datagrid_id, datagrid_name, datagrid_id
                );

                legacy_select_clause := base_select_clause;
                current_select_clause := base_select_clause;

                FOR i IN 0..(jsonb_array_length(mapping_rows) - 1) LOOP
                    row_data := mapping_rows->i;
                    type_path := row_data->>'TypePath';
                    IF (row_data->>'Parent')::boolean = true OR type_path IS NULL OR NOT (type_path LIKE '%datagrid%') THEN CONTINUE; END IF;

                    column_name := row_data->>'ColumnName';
                    column_type := lower(COALESCE(row_data->>'Type', 'text'));
                    data_path_raw := row_data->>'DataPath';

                    -- Version gating                    
                    IF (row_data->>'VersionLabel') IS NOT NULL 
                    AND NOT (version_lbl = ANY(string_to_array((row_data->>'VersionLabel'), ', '))) THEN
                        CONTINUE;
                    END IF;

                    -- Check if this field belongs to the current datagrid
                    IF data_path_raw ~ '^\(DK[0-9]+\)' THEN
                        IF split_part(regexp_replace(data_path_raw, '^\(DK[0-9]+\)', ''), '->', 1) != datagrid_name THEN CONTINUE; END IF;
                        field_name := split_part(regexp_replace(data_path_raw, '^\(DK[0-9]+\)', ''), '->', 2);
                    ELSE
                        IF split_part(data_path_raw, '->', 1) != datagrid_name THEN CONTINUE; END IF;
                        field_name := split_part(data_path_raw, '->', 2);
                    END IF;

                    json_path := format('->''%s''', trim(field_name));
                    -- Same source prefix for both schemas since datagrids use lateral joins
                    legacy_source_prefix := 'dg_' || datagrid_id || '_tbl.dg_' || datagrid_id;
                    current_source_prefix := 'dg_' || datagrid_id || '_tbl.dg_' || datagrid_id;

                    CASE column_type
                        WHEN 'textfield', 'textarea', 'email', 'select', 'phonenumber' THEN
                            json_path := replace(json_path, '->', '->>');
                            data_path := format('(%s%s)', '{}', json_path);
                            legacy_select_clause := replace(legacy_select_clause, format('NULL::TEXT AS %I', column_name), format('%s AS %I', replace(data_path, '{}', legacy_source_prefix), column_name));
                            current_select_clause := replace(current_select_clause, format('NULL::TEXT AS %I', column_name), format('%s AS %I', replace(data_path, '{}', current_source_prefix), column_name));
                        WHEN 'number' THEN
                            json_path := replace(json_path, '->', '->>');
                            data_path := format('(CASE WHEN (%s%s) IS NULL THEN NULL WHEN (%s%s) ~ ''^-?[0-9]+\.?[0-9]*$'' THEN (%s%s)::NUMERIC ELSE NULL END)', '{}', json_path, '{}', json_path, '{}', json_path);
                            legacy_select_clause := replace(legacy_select_clause, format('NULL::NUMERIC AS %I', column_name), format('%s AS %I', replace(data_path, '{}', legacy_source_prefix), column_name));
                            current_select_clause := replace(current_select_clause, format('NULL::NUMERIC AS %I', column_name), format('%s AS %I', replace(data_path, '{}', current_source_prefix), column_name));
                        WHEN 'currency' THEN
                            json_path := replace(json_path, '->', '->>');
                            data_path := format('(CASE WHEN (%s%s) IS NULL THEN NULL WHEN (%s%s) ~ ''^-?[0-9]+\.?[0-9]*$'' THEN (%s%s)::DECIMAL(18,2) ELSE NULL END)', '{}', json_path, '{}', json_path, '{}', json_path);
                            legacy_select_clause := replace(legacy_select_clause, format('NULL::DECIMAL(18,2) AS %I', column_name), format('%s AS %I', replace(data_path, '{}', legacy_source_prefix), column_name));
                            current_select_clause := replace(current_select_clause, format('NULL::DECIMAL(18,2) AS %I', column_name), format('%s AS %I', replace(data_path, '{}', current_source_prefix), column_name));
                        WHEN 'option', 'checkbox' THEN
                            json_path := replace(json_path, '->', '->>');
                            data_path := format('(CASE WHEN (%s%s) IS NULL THEN NULL WHEN (%s%s) ~ ''^(true|false|t|f|1|0|yes|no)$'' THEN CASE WHEN lower((%s%s)) IN (''true'', ''t'', ''1'', ''yes'') THEN true WHEN lower((%s%s)) IN (''false'', ''f'', ''0'', ''no'') THEN false ELSE NULL END ELSE NULL END)',
                                '{}', json_path, '{}', json_path, '{}', json_path, '{}', json_path);
                            legacy_select_clause := replace(legacy_select_clause, format('NULL::BOOLEAN AS %I', column_name), format('%s AS %I', replace(data_path, '{}', legacy_source_prefix), column_name));
                            current_select_clause := replace(current_select_clause, format('NULL::BOOLEAN AS %I', column_name), format('%s AS %I', replace(data_path, '{}', current_source_prefix), column_name));
                        ELSE
                            json_path := replace(json_path, '->', '->>');
                            data_path := format('(%s%s)', '{}', json_path);
                            legacy_select_clause := replace(legacy_select_clause, format('NULL::TEXT AS %I', column_name), format('%s AS %I', replace(data_path, '{}', legacy_source_prefix), column_name));
                            current_select_clause := replace(current_select_clause, format('NULL::TEXT AS %I', column_name), format('%s AS %I', replace(data_path, '{}', current_source_prefix), column_name));
                    END CASE;
                END LOOP;

                all_queries := all_queries || format(
                    '(SELECT %s FROM %s WHERE afs."ApplicationFormVersionId" = %L AND afs."Submission"->''submission''->''submission'' IS NULL AND dg_%s_tbl.dg_%s != ''null''::jsonb) UNION ALL (SELECT %s FROM %s WHERE afs."ApplicationFormVersionId" = %L AND afs."Submission"->''submission''->''submission'' IS NOT NULL AND dg_%s_tbl.dg_%s != ''null''::jsonb)',
                    legacy_select_clause, legacy_from_clause, version_id, datagrid_id, datagrid_id,
                    current_select_clause, current_from_clause, version_id, datagrid_id, datagrid_id
                );
            END LOOP;
        END IF;

    END LOOP;

    RETURN array_to_string(all_queries, ' UNION ALL ');
END;
$function$;
