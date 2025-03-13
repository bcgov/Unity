-- 1. Create Readonly and Read/Write Group Roles
DO $$ 
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'metabase_readonly') THEN
        CREATE ROLE metabase_readonly NOLOGIN;
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'metabase_readwrite') THEN
        CREATE ROLE metabase_readwrite NOLOGIN;
    END IF;
END $$;

-- 2. Create Users and Assign Them to the Correct Roles
DO $$
DECLARE
    ugm_readonly_password TEXT := (SELECT string_agg(substring('ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789' FROM floor(random() * 62 + 1)::int FOR 1), '') FROM generate_series(1, 16));
    ugt_readonly_password TEXT := (SELECT string_agg(substring('ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789' FROM floor(random() * 62 + 1)::int FOR 1), '') FROM generate_series(1, 16));
    ugm_uploads_password TEXT := (SELECT string_agg(substring('ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789' FROM floor(random() * 62 + 1)::int FOR 1), '') FROM generate_series(1, 16));
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'ugm_readonly') THEN
        EXECUTE format('CREATE ROLE ugm_readonly WITH LOGIN PASSWORD %L INHERIT', ugm_readonly_password);
        GRANT metabase_readonly TO ugm_readonly;
        RAISE NOTICE 'Role ugm_readonly created and assigned to metabase_readonly successfully. Password: %', ugm_readonly_password;
    ELSE
        RAISE NOTICE 'Role ugm_readonly already exists.';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'ugt_readonly') THEN
        EXECUTE format('CREATE ROLE ugt_readonly WITH LOGIN PASSWORD %L INHERIT', ugt_readonly_password);
        GRANT metabase_readonly TO ugt_readonly;
        RAISE NOTICE 'Role ugt_readonly created and assigned to metabase_readonly successfully. Password: %', ugt_readonly_password;
    ELSE
        RAISE NOTICE 'Role ugt_readonly already exists.';
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'ugm_uploads') THEN
        EXECUTE format('CREATE ROLE ugm_uploads WITH LOGIN PASSWORD %L INHERIT', ugm_uploads_password);
        GRANT metabase_readwrite TO ugm_uploads;
        RAISE NOTICE 'Role ugm_uploads created and assigned to metabase_readwrite successfully. Password: %', ugm_uploads_password;
    ELSE
        RAISE NOTICE 'Role ugm_uploads already exists.';
    END IF;
END $$;

-- 3. Cleanup Roles
DO $$ 
BEGIN
    -- Role: metabase_grant_name
    DROP ROLE IF EXISTS metabase_grant_name;
    -- Role: grant_name
    DROP ROLE IF EXISTS grant_name;
    -- Role: pg_read_all_data
    REVOKE pg_read_all_data FROM metabase_readonly;
    REVOKE pg_write_all_data FROM metabase_readwrite;
END $$;

-- 4. Verify Role Assignments
-- List all custom roles excluding default PostgreSQL roles
SELECT rolname, rolsuper, rolinherit, rolcreaterole, rolcreatedb, rolcanlogin, rolreplication, rolbypassrls
FROM pg_roles
WHERE rolname NOT LIKE 'pg_%';

-- 5. Verify Role Memberships:
-- List role memberships excluding default PostgreSQL roles
SELECT pg_roles.rolname AS role_name, member.rolname AS member_name
FROM pg_auth_members
JOIN pg_roles ON pg_roles.oid = pg_auth_members.roleid
JOIN pg_roles AS member ON member.oid = pg_auth_members.member
WHERE pg_roles.rolname NOT LIKE 'pg_%'
AND member.rolname NOT LIKE 'pg_%';