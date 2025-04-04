-- This script ensures that the metabase_readwrite role has the necessary CONNECT, USAGE, SELECT, INSERT, UPDATE, and DELETE privileges 
-- on the public schema. It also sets default privileges for any new tables and sequences created in the public schema. 

DO $$ 
DECLARE 
    db_name TEXT := 'metabaseuploaddb';
    schema TEXT := 'public';
BEGIN
    -- Grant CONNECT and TEMPORARY on the database to metabase_readwrite
    EXECUTE format('GRANT CONNECT, TEMPORARY ON DATABASE %I TO metabase_readwrite;', db_name);
    RAISE NOTICE 'Granted CONNECT, TEMPORARY on database % to role metabase_readwrite', db_name;

    -- Grant USAGE and CREATE on the public schema to metabase_readwrite
    EXECUTE format('GRANT USAGE, CREATE ON SCHEMA %I TO metabase_readwrite;', schema);
    RAISE NOTICE 'Granted USAGE, CREATE on schema % to role metabase_readwrite', schema;

    -- Grant SELECT, INSERT, UPDATE, DELETE on all existing tables in the public schema
    EXECUTE format('GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA %I TO metabase_readwrite;', schema);
    RAISE NOTICE 'Granted SELECT, INSERT, UPDATE, DELETE on all tables in schema % to role metabase_readwrite', schema;

    -- Grant USAGE and SELECT, UPDATE on all sequences in the public schema
    EXECUTE format('GRANT USAGE, SELECT, UPDATE ON ALL SEQUENCES IN SCHEMA %I TO metabase_readwrite;', schema);
    RAISE NOTICE 'Granted USAGE, SELECT, UPDATE on all sequences in schema % to role metabase_readwrite', schema;

    -- Set default privileges for metabase_readwrite
    EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO metabase_readwrite;', schema);
    EXECUTE format('ALTER DEFAULT PRIVILEGES IN SCHEMA %I GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO metabase_readwrite;', schema);
    RAISE NOTICE 'Set default privileges for role metabase_readwrite in schema %', schema;
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