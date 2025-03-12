-- This script sets up the metabaseuploaddb with the necessary privileges for the metabase_dbuser role.
DO $$
DECLARE
    db_name TEXT := 'metabaseuploaddb';
BEGIN
    -- Check if the database exists and print the appropriate message
    IF NOT EXISTS (SELECT FROM pg_database WHERE datname = db_name) THEN
        RAISE NOTICE 'Database does not exist. You need to create it manually: CREATE DATABASE %;', db_name;
    ELSE
        RAISE NOTICE 'Database "%" already exists.', db_name;
    END IF;
END $$;

DO $$ 
DECLARE 
    db_name TEXT := 'metabaseuploaddb';
    schema TEXT := 'public';
BEGIN
    -- Grant ALL PRIVILEGES on the database to metabase_dbuser
    EXECUTE format('GRANT ALL PRIVILEGES ON DATABASE %I TO metabase_dbuser;', db_name);
    RAISE NOTICE 'Granted ALL PRIVILEGES on database % to role metabase_dbuser', db_name;

    -- Grant ALL on the public schema to metabase_dbuser
    EXECUTE format('GRANT ALL ON SCHEMA %I TO metabase_dbuser;', schema);
    RAISE NOTICE 'Granted ALL on schema % to role metabase_dbuser', schema;

    -- Alter the database owner to metabase_dbuser
    EXECUTE format('ALTER DATABASE %I OWNER TO metabase_dbuser;', db_name);
    RAISE NOTICE 'Changed owner of database % to metabase_dbuser', db_name;

    -- Grant USAGE and CREATE on the public schema to metabase_dbuser
    EXECUTE format('GRANT USAGE, CREATE ON SCHEMA %I TO metabase_dbuser;', schema);
    RAISE NOTICE 'Granted USAGE, CREATE on schema % to role metabase_dbuser', schema;
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
