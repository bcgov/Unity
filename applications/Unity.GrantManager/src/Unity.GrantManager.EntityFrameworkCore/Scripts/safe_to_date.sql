CREATE OR REPLACE FUNCTION "Reporting".safe_to_date(val text)
 RETURNS date
 LANGUAGE plpgsql
 STABLE
AS $function$
DECLARE
    normalized text;
    result date;
    parts text[];
    p1 int;
    p2 int;
    p3 int;
BEGIN
    IF val IS NULL OR trim(val) = '' THEN
        RETURN NULL;
    END IF;

    normalized := trim(val);

    -- Direct cast handles ISO 8601 (YYYY-MM-DD) and is strict about invalid dates
    BEGIN
        RETURN normalized::date;
    EXCEPTION WHEN OTHERS THEN
        NULL;
    END;

    -- Branch by detected layout; validate parsed results to reject
    -- silently-wrapped invalid dates (e.g., month 13 rolling into next year)
    IF normalized ~ '^\d{4}/' THEN
        -- Year-first: YYYY/MM/DD
        BEGIN
            parts := string_to_array(normalized, '/');
            p1 := parts[1]::int; p2 := parts[2]::int; p3 := parts[3]::int;
            result := to_date(normalized, 'YYYY/MM/DD');
        EXCEPTION WHEN OTHERS THEN
            RETURN NULL;
        END;
        IF EXTRACT(year FROM result) = p1
           AND EXTRACT(month FROM result) = p2
           AND EXTRACT(day FROM result) = p3 THEN
            RETURN result;
        END IF;

    ELSIF normalized ~ '^\d{1,2}/\d{1,2}/\d{4}' THEN
        -- Extract date parts once (regex guarantees slash-separated integers)
        BEGIN
            parts := string_to_array(normalized, '/');
            p1 := parts[1]::int; p2 := parts[2]::int; p3 := parts[3]::int;
        EXCEPTION WHEN OTHERS THEN
            RETURN NULL;
        END;

        -- Try MM/DD/YYYY (North American locale used in BC)
        BEGIN result := to_date(normalized, 'MM/DD/YYYY'); EXCEPTION WHEN OTHERS THEN result := NULL; END;
        IF result IS NOT NULL
           AND EXTRACT(month FROM result) = p1
           AND EXTRACT(day FROM result) = p2
           AND EXTRACT(year FROM result) = p3 THEN
            RETURN result;
        END IF;

        -- Try DD/MM/YYYY
        BEGIN result := to_date(normalized, 'DD/MM/YYYY'); EXCEPTION WHEN OTHERS THEN result := NULL; END;
        IF result IS NOT NULL
           AND EXTRACT(day FROM result) = p1
           AND EXTRACT(month FROM result) = p2
           AND EXTRACT(year FROM result) = p3 THEN
            RETURN result;
        END IF;
    END IF;

    -- All attempts failed, return NULL gracefully
    RETURN NULL;
END;
$function$;
