CREATE OR REPLACE FUNCTION "Reporting".safe_to_date(val text)
 RETURNS date
 LANGUAGE plpgsql
 IMMUTABLE
AS $function$
DECLARE
    normalized text;
BEGIN
    IF val IS NULL OR trim(val) = '' THEN
        RETURN NULL;
    END IF;

    normalized := trim(val);

    -- Try direct cast first (handles ISO 8601 YYYY-MM-DD, etc.)
    BEGIN
        RETURN normalized::date;
    EXCEPTION WHEN OTHERS THEN
        NULL;
    END;

    -- Try MM/DD/YYYY
    BEGIN
        RETURN to_date(normalized, 'MM/DD/YYYY');
    EXCEPTION WHEN OTHERS THEN
        NULL;
    END;

    -- Try DD/MM/YYYY
    BEGIN
        RETURN to_date(normalized, 'DD/MM/YYYY');
    EXCEPTION WHEN OTHERS THEN
        NULL;
    END;

    -- Try YYYY/MM/DD
    BEGIN
        RETURN to_date(normalized, 'YYYY/MM/DD');
    EXCEPTION WHEN OTHERS THEN
        NULL;
    END;

    -- All attempts failed, return NULL gracefully
    RETURN NULL;
END;
$function$;
