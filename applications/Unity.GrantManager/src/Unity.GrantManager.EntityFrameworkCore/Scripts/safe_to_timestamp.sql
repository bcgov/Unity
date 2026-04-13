CREATE OR REPLACE FUNCTION "Reporting".safe_to_timestamp(val text)
 RETURNS timestamp without time zone
 LANGUAGE plpgsql
 IMMUTABLE
AS $function$
DECLARE
    normalized text;
BEGIN
    IF val IS NULL OR trim(val) = '' THEN
        RETURN NULL;
    END IF;

    -- Normalize: trim whitespace and handle dotted meridian indicators
    normalized := trim(val);
    normalized := regexp_replace(normalized, '\s*[aA]\.[mM]\.', ' AM', 'g');
    normalized := regexp_replace(normalized, '\s*[pP]\.[mM]\.', ' PM', 'g');

    -- Try direct cast first (handles ISO 8601, standard YYYY-MM-DD HH24:MI:SS, etc.)
    BEGIN
        RETURN normalized::timestamp;
    EXCEPTION WHEN OTHERS THEN
        NULL;
    END;

    -- Try MM/DD/YYYY with 12-hour clock
    BEGIN
        RETURN to_timestamp(normalized, 'MM/DD/YYYY HH12:MI:SS AM');
    EXCEPTION WHEN OTHERS THEN
        NULL;
    END;

    -- Try MM/DD/YYYY with 24-hour clock
    BEGIN
        RETURN to_timestamp(normalized, 'MM/DD/YYYY HH24:MI:SS');
    EXCEPTION WHEN OTHERS THEN
        NULL;
    END;

    -- Try DD/MM/YYYY with 12-hour clock
    BEGIN
        RETURN to_timestamp(normalized, 'DD/MM/YYYY HH12:MI:SS AM');
    EXCEPTION WHEN OTHERS THEN
        NULL;
    END;

    -- Try YYYY/MM/DD with 24-hour clock
    BEGIN
        RETURN to_timestamp(normalized, 'YYYY/MM/DD HH24:MI:SS');
    EXCEPTION WHEN OTHERS THEN
        NULL;
    END;

    -- All attempts failed, return NULL gracefully
    RETURN NULL;
END;
$function$;
