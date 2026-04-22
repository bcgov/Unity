CREATE OR REPLACE FUNCTION "Reporting".safe_to_timestamp(val text)
 RETURNS timestamp without time zone
 LANGUAGE plpgsql
 STABLE
AS $function$
DECLARE
    normalized text;
    has_meridian boolean;
    has_seconds boolean;
    has_time boolean;
    result timestamp;
    date_part text;
    parts text[];
    p1 int;
    p2 int;
    p3 int;
    fmt text;
    fmt_mdy text;
    fmt_dmy text;
BEGIN
    IF val IS NULL OR trim(val) = '' THEN
        RETURN NULL;
    END IF;

    -- Normalize: trim whitespace and handle dotted meridian indicators
    normalized := trim(val);
    normalized := regexp_replace(normalized, '\s*[aA]\.[mM]\.', ' AM', 'g');
    normalized := regexp_replace(normalized, '\s*[pP]\.[mM]\.', ' PM', 'g');

    -- Try direct cast first (handles ISO 8601, YYYY-MM-DD HH24:MI:SS, etc.)
    BEGIN
        RETURN normalized::timestamp;
    EXCEPTION WHEN OTHERS THEN
        NULL;
    END;

    -- Pre-detect format characteristics via regex to select the right parse
    -- branch and avoid entering unnecessary BEGIN...EXCEPTION blocks
    has_meridian := normalized ~* '\s+(AM|PM)\s*$';
    has_time := normalized ~ '\s+\d{1,2}:\d{2}';
    has_seconds := normalized ~ '\d{1,2}:\d{2}:\d{2}';

    -- Year-first with slash: YYYY/MM/DD variants
    IF normalized ~ '^\d{4}/' THEN
        date_part := split_part(normalized, ' ', 1);
        BEGIN
            parts := string_to_array(date_part, '/');
            p1 := parts[1]::int; p2 := parts[2]::int; p3 := parts[3]::int;
        EXCEPTION WHEN OTHERS THEN
            RETURN NULL;
        END;

        IF has_time AND has_seconds THEN
            fmt := 'YYYY/MM/DD HH24:MI:SS';
        ELSIF has_time THEN
            fmt := 'YYYY/MM/DD HH24:MI';
        ELSE
            fmt := 'YYYY/MM/DD';
        END IF;

        BEGIN result := to_timestamp(normalized, fmt); EXCEPTION WHEN OTHERS THEN result := NULL; END;
        IF result IS NOT NULL
           AND EXTRACT(year FROM result) = p1
           AND EXTRACT(month FROM result) = p2
           AND EXTRACT(day FROM result) = p3 THEN
            RETURN result;
        END IF;
        RETURN NULL;
    END IF;

    -- Slash-separated with year last: MM/DD/YYYY or DD/MM/YYYY
    -- MM/DD is tried first (North American locale used in BC)
    IF normalized ~ '^\d{1,2}/\d{1,2}/\d{4}' THEN
        date_part := split_part(normalized, ' ', 1);
        BEGIN
            parts := string_to_array(date_part, '/');
            p1 := parts[1]::int; p2 := parts[2]::int; p3 := parts[3]::int;
        EXCEPTION WHEN OTHERS THEN
            RETURN NULL;
        END;

        -- Select format strings based on detected time characteristics
        IF has_time AND has_seconds AND has_meridian THEN
            fmt_mdy := 'MM/DD/YYYY HH12:MI:SS AM';
            fmt_dmy := 'DD/MM/YYYY HH12:MI:SS AM';
        ELSIF has_time AND has_seconds THEN
            fmt_mdy := 'MM/DD/YYYY HH24:MI:SS';
            fmt_dmy := 'DD/MM/YYYY HH24:MI:SS';
        ELSIF has_time AND has_meridian THEN
            fmt_mdy := 'MM/DD/YYYY HH12:MI AM';
            fmt_dmy := 'DD/MM/YYYY HH12:MI AM';
        ELSIF has_time THEN
            fmt_mdy := 'MM/DD/YYYY HH24:MI';
            fmt_dmy := 'DD/MM/YYYY HH24:MI';
        ELSE
            fmt_mdy := 'MM/DD/YYYY';
            fmt_dmy := 'DD/MM/YYYY';
        END IF;

        -- Try MM/DD/YYYY, validate parsed date parts match input
        BEGIN result := to_timestamp(normalized, fmt_mdy); EXCEPTION WHEN OTHERS THEN result := NULL; END;
        IF result IS NOT NULL
           AND EXTRACT(month FROM result) = p1
           AND EXTRACT(day FROM result) = p2
           AND EXTRACT(year FROM result) = p3 THEN
            RETURN result;
        END IF;

        -- Try DD/MM/YYYY, validate parsed date parts match input
        BEGIN result := to_timestamp(normalized, fmt_dmy); EXCEPTION WHEN OTHERS THEN result := NULL; END;
        IF result IS NOT NULL
           AND EXTRACT(day FROM result) = p1
           AND EXTRACT(month FROM result) = p2
           AND EXTRACT(year FROM result) = p3 THEN
            RETURN result;
        END IF;

        RETURN NULL;
    END IF;

    -- All attempts failed, return NULL gracefully
    RETURN NULL;
END;
$function$;
