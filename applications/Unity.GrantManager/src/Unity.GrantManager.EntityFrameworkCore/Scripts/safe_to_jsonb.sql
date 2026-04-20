CREATE OR REPLACE FUNCTION "Reporting".safe_to_jsonb(val text)
 RETURNS jsonb
 LANGUAGE plpgsql
 STABLE
AS $function$
BEGIN
    IF val IS NULL OR btrim(val) = '' THEN
        RETURN NULL;
    END IF;

    BEGIN
        RETURN val::jsonb;
    EXCEPTION WHEN OTHERS THEN
        RETURN NULL;
    END;
END;
$function$;
