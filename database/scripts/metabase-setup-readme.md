## Metabase Read-Only Permissions in PostgreSQL

The script applies **read-only permissions** to the schemas relevant to Metabase reporting (`public`, `Flex`, `Notifications`, `Payments`). It does the following:
1. **Checks if each schema exists** before granting permissions.
2. **Grants USAGE on schemas** to `metabase_readonly`.
3. **Sets default privileges** for `metabase_readonly`:
   - **TABLES:** Grants `SELECT`
   - **SEQUENCES:** Grants `USAGE, SELECT`
4. **Lists existing privileges** for schemas, tables, and sequences.

---

## Database Setup Roles

### 1. Running `metabase-setup-roles.sql`

This script creates the necessary roles and users for Metabase with read-only and read/write permissions.

#### Steps:
1. **Create Readonly and Read/Write Group Roles**:
    - `metabase_readonly`
    - `metabase_readwrite`
2. **Create Users and Assign Them to the Correct Roles**:
    - `ugm_readonly`
    - `ugt_readonly`
    - `ugm_uploads`
3. **Cleanup Roles**:
    - Drop unnecessary roles.
4. **Verify Role Assignments**:
    - List all custom roles excluding default PostgreSQL roles.
5. **Verify Role Memberships**:
    - List role memberships excluding default PostgreSQL roles.

### 2. Applying `metabase_readonly` to All Tenant Databases

After running `metabase-setup-roles.sql`, apply the `metabase_readonly` role to all tenant databases to ensure read-only access.

#### Steps:
1. **Grant CONNECT privilege on the database**.
2. **Grant USAGE on schemas**.
3. **Grant SELECT on all existing tables in the schemas**.
4. **Grant USAGE and SELECT on all sequences in the schemas**.
5. **Set default privileges for `metabase_readonly`**:
    - **TABLES:** Grants `SELECT`
    - **SEQUENCES:** Grants `USAGE, SELECT`

### 3. Running `metabase-setup-metabaseuploaddb.sql`

This script sets up the `metabaseuploaddb` with the necessary privileges for the `metabase_dbuser` role.

#### Steps:
1. **Grant ALL PRIVILEGES on the database to `metabase_dbuser`**.
2. **Grant ALL on the public schema to `metabase_dbuser`**.
3. **Alter the database owner to `metabase_dbuser`**.
4. **Grant USAGE and CREATE on the public schema to `metabase_dbuser`**.

### 4. Applying `metabase_readwrite` Role

After setting up the `metabaseuploaddb`, apply the `metabase_readwrite` role to ensure the necessary privileges.

#### Steps:
1. **Grant CONNECT and TEMPORARY on the database to `metabase_readwrite`**.
2. **Grant USAGE and CREATE on the public schema to `metabase_readwrite`**.
3. **Grant SELECT, INSERT, UPDATE, DELETE on all existing tables in the public schema**.
4. **Grant USAGE and SELECT, UPDATE on all sequences in the public schema**.
5. **Set default privileges for `metabase_readwrite`**:
    - **TABLES:** Grants `SELECT, INSERT, UPDATE, DELETE`
    - **SEQUENCES:** Grants `USAGE, SELECT, UPDATE`

---

### Explanation of Query Results
Each row in the output represents a privilege assignment for a specific schema and object type (`SCHEMA`, `TABLE`, `SEQUENCE`).

#### **Key Terms**
- **`object_type`**: Type of object (SCHEMA, TABLE, SEQUENCE).
- **`schema`**: The schema name.
- **`owner`**: The user who owns the schema.
- **`privileges`**: The privileges assigned in `[role=permissions/owner]` format.
  - `r` = SELECT (read)
  - `U` = USAGE
  - `C` = CREATE (for schemas)
  - `rU` = SELECT + USAGE (for sequences)
  - `UC` = USAGE + CREATE (for schemas)

#### **Results Breakdown**
| Object Type | Schema        | Owner     | Privileges |
|-------------|--------------|-----------|------------|
| **TABLE**   | `Flex`        | `postgres` | `["metabase_readonly=r/postgres"]` → Read-only access on tables |
| **SCHEMA**  | `Flex`        | `postgres` | `["postgres=UC/postgres", "metabase_readonly=U/postgres"]` → `metabase_readonly` can use this schema, but not create objects. |
| **SEQUENCE**| `Flex`        | `postgres` | `["metabase_readonly=rU/postgres"]` → Read and use sequences. |
| **SCHEMA**  | `Notifications` | `postgres` | `["postgres=UC/postgres", "metabase_readonly=U/postgres"]` |
| **SEQUENCE**| `Notifications` | `postgres` | `["metabase_readonly=rU/postgres"]` |
| **TABLE**   | `Notifications` | `postgres` | `["metabase_readonly=r/postgres"]` |
| **SCHEMA**  | `Payments` | `postgres` | `["postgres=UC/postgres", "metabase_readonly=U/postgres"]` |
| **TABLE**   | `Payments` | `postgres` | `["metabase_readonly=r/postgres"]` |
| **SEQUENCE**| `Payments` | `postgres` | `["metabase_readonly=rU/postgres"]` |
| **SCHEMA**  | `public` | `pg_database_owner` | `["pg_database_owner=UC/pg_database_owner", "=U/pg_database_owner", "metabase_readonly=U/pg_database_owner"]` |
| **TABLE**   | `public` | `postgres` | `["metabase_readonly=r/postgres"]` |
| **SEQUENCE**| `public` | `postgres` | `["metabase_readonly=rU/postgres"]` |

---

### **Key Takeaways**
- `metabase_readonly` **has access to all specified schemas** (`USAGE` granted).
- `metabase_readonly` **can query tables (`SELECT`) but cannot modify them**.
- `metabase_readonly` **can use sequences (`USAGE, SELECT`) but cannot modify them**.
- **No `CREATE` privileges were granted**, ensuring Metabase remains read-only.

This configuration ensures **secure, repeatable, and limited** readonly PostgreSQL access for Metabase reporting.
