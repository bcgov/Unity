-- This script ensures that the metabase_readonly role has the necessary CONNECT, USAGE, and SELECT privileges 
-- on the database, schemas, tables, and sequences. It also sets default privileges for any new tables and sequences 
-- created in the specified schemas. This should allow the metabase_readonly role to access the schemas and tables 
-- with read-only permissions
DO $$
DECLARE 
    db_name TEXT;
    schema TEXT;
    schema_list TEXT[] := ARRAY['public', 'Flex', 'Notifications', 'Payments', 'Reporting'];
    existing_schemas TEXT := '';
BEGIN
    -- Get the name of the current database
    SELECT current_database() INTO db_name;

    -- Grant CONNECT privilege on the database
    EXECUTE format('GRANT CONNECT ON DATABASE %I TO metabase_readonly;', db_name);
    RAISE NOTICE 'Granted CONNECT on database % to role metabase_readonly', db_name;

    -- List schemas in the current database
    RAISE NOTICE 'Listing schemas in the current database %:', db_name;
    FOREACH schema IN ARRAY schema_list LOOP
        IF EXISTS (SELECT 1 FROM information_schema.schemata s WHERE s.schema_name = schema) THEN
            existing_schemas := existing_schemas || schema || ', ';
        END IF;
    END LOOP;

    -- Remove the trailing comma and space
    IF existing_schemas <> '' THEN
        existing_schemas := substring(existing_schemas FROM 1 FOR length(existing_schemas) - 2);
    END IF;

    RAISE NOTICE 'Schemas in the current database %: %', db_name, existing_schemas;

    -- Grant schema usage and set default privileges for metabase_readonly
    FOREACH schema IN ARRAY schema_list LOOP
        IF EXISTS (SELECT 1 FROM information_schema.schemata s WHERE s.schema_name = schema) THEN
            EXECUTE format('GRANT USAGE ON SCHEMA %I TO metabase_readonly;', schema);
            RAISE NOTICE 'Granted USAGE on schema % to role metabase_readonly', schema;

            -- Grant SELECT on all existing tables in the schema
            EXECUTE format('GRANT SELECT ON ALL TABLES IN SCHEMA %I TO metabase_readonly;', schema);
            RAISE NOTICE 'Granted SELECT on all tables in schema % to role metabase_readonly', schema;

            -- Grant USAGE and SELECT on all sequences in the schema
            EXECUTE format('GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA %I TO metabase_readonly;', schema);
            RAISE NOTICE 'Granted USAGE and SELECT on all sequences in schema % to role metabase_readonly', schema;

            -- Set default privileges for metabase_readonly
            EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT SELECT ON TABLES TO metabase_readonly;', schema);
            EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT USAGE, SELECT ON SEQUENCES TO metabase_readonly;', schema);
            RAISE NOTICE 'Set default privileges for role metabase_readonly in schema %', schema;
        ELSE
            RAISE NOTICE 'Schema % does not exist in the current database', schema;
        END IF;
    END LOOP;
END $$;

-- Combined Query to List Schema Privileges and Default Privileges for All Schemas, Sorted by Schema Name
WITH schema_privileges AS (
    SELECT 
        'SCHEMA' AS object_type,
        nspname AS schema,
        pg_catalog.pg_get_userbyid(nspowner) AS owner,
        array_agg(acl) AS privileges
    FROM 
        pg_namespace
    LEFT JOIN 
        pg_roles ON pg_roles.oid = pg_namespace.nspowner
    LEFT JOIN 
        unnest(nspacl) AS acl ON true
    WHERE 
        nspname NOT LIKE 'pg_%' AND nspname <> 'information_schema'
    GROUP BY 
        nspname, nspowner
),
default_privileges AS (
    SELECT 
        CASE defaclobjtype
            WHEN 'r' THEN 'TABLE'
            WHEN 'S' THEN 'SEQUENCE'
            WHEN 'f' THEN 'FUNCTION'
            WHEN 'T' THEN 'TYPE'
        END AS object_type,
        nspname AS schema,
        pg_catalog.pg_get_userbyid(defaclrole) AS role,
        defaclacl AS privileges
    FROM 
        pg_default_acl
    JOIN 
        pg_namespace ON pg_namespace.oid = pg_default_acl.defaclnamespace
    WHERE 
        defaclobjtype IN ('r', 'S', 'f', 'T')
        AND nspname NOT LIKE 'pg_%' AND nspname <> 'information_schema'
)
SELECT * FROM schema_privileges
UNION ALL
SELECT * FROM default_privileges
ORDER BY schema;