# Migration history resets (squashing)

This project squashes `HostMigrations` and `TenantMigrations` down to a single
`Initial` migration per context periodically, once the migration list has grown
long enough that it's more noise than history. This document covers how that
was done (2026-07), the mechanism that makes it safe for already-deployed
databases, and what to avoid in future migrations so the next reset is easier.

## Why this isn't a plain "delete files, add Initial"

`Database.MigrateAsync()` reads `__EFMigrationsHistory` **once** at the start
of the call to compute the full list of pending migrations, then runs that
fixed list end to end. It does not re-check history between migrations within
the same call.

That rules out the obvious approach of shipping two migrations — a "Squash"
that clears history and stamps itself, followed by the real "Initial" — and
relying on EF to skip "Initial" on already-migrated databases. By the time
"Squash" runs, EF has *already* decided to run "Initial" next in that same
call, so on an existing database "Initial" would still fire and fail on the
first `CreateTable` that already exists.

## The mechanism actually used

`EntityFrameworkCoreGrantManagerDbSchemaMigrator.ReconcileMigrationHistoryAsync`
runs raw SQL directly against the target database, **before** `MigrateAsync()`
is called (for both the host DB and each tenant DB) — not as an EF migration
competing for the same pending-list computation:

- If `__EFMigrationsHistory` has any row other than the current `Initial`
  migration id, the database predates the squash: history is wiped and
  replaced with a single row stamping `Initial` as already applied. EF then
  sees nothing pending and `MigrateAsync()` is a no-op.
- If the history table doesn't exist yet, or is already empty/already stamped,
  nothing happens. This is the case for a brand-new database — including any
  tenant provisioned after the squash — so `MigrateAsync()` proceeds normally
  and runs `Initial` for real, building the schema from scratch.

This is the same "detect-and-fix, safe to run on every deploy forever"
pattern already used by `TenantConnectionStringEncryptionMigrator` — it's
called unconditionally every run and is a no-op once there's nothing left to
reconcile.

Guard, roughly:

```sql
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = '__EFMigrationsHistory') THEN
        IF EXISTS (SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" <> '<InitialId>') THEN
            DELETE FROM "__EFMigrationsHistory";
            INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
            VALUES ('<InitialId>', '<EfCoreVersion>');
        END IF;
    END IF;
END $$;
```

The host and tenant `Initial` migration ids are hardcoded constants at the top
of `EntityFrameworkCoreGrantManagerDbSchemaMigrator` (`HostInitialMigrationId`,
`TenantInitialMigrationId`) — **update these if the squash is ever redone**.

### A gotcha this surfaced

On a genuinely empty Postgres instance, the target database itself
(`UnityGrantManager`) doesn't exist yet. `MigrateAsync()` would normally
create it as its first step, but `ReconcileMigrationHistoryAsync` needs to
connect *before* that. The host path didn't previously need a
"create database if missing" step (only the tenant path did, since tenant
databases are always dynamically provisioned) — it now does, mirroring the
tenant path's `CanConnectAsync()` / `IRelationalDatabaseCreator.CreateAsync()`
check. If this pattern is copied elsewhere, keep that ordering in mind.

## How to test a reset locally

Two genuinely different code paths need checking:

1. **Already-migrated database** (e.g. your normal local dev DB, or a restore
   of one): run the migrator against it. It should complete with no errors,
   and `__EFMigrationsHistory` should end with exactly one row (`Initial`).
2. **Brand-new, empty Postgres instance**: run the migrator against it.
   `Initial` should build the full schema for real (this is the path that
   exercises the "create database if missing" step above).

Then `pg_dump --schema-only` both resulting databases and diff them — they
should be structurally identical. This also catches model/snapshot drift
before it reaches a shared environment (see below).

## `dotnet ef migrations add` cannot see database functions/procedures/extensions

This is the single biggest gap the 2026-07 squash hit, and it's easy to miss:
`dotnet ef migrations add` only diffs the C#-declared entity model (tables,
columns, indexes, keys). It has **no visibility into anything created via
`migrationBuilder.Sql(...)`** — `CREATE FUNCTION`, `CREATE PROCEDURE`,
`CREATE EXTENSION`, standalone views, etc. all vanish silently when migrations
are squashed, because the regenerated `Initial` migration is just a fresh
diff of the model, not a replay of history.

The tenant DB alone had 18 functions/procedures in the `Reporting` and
`public` schemas (the entire dynamic view-generation engine described in
`documentation/reporting/` — see `GenerateViewBackgroundJob`) plus the
`fuzzystrmatch` and `pg_stat_statements` extensions, none of which existed on
a freshly-squashed database until this was caught by manually diffing a real
database against a fresh one. The host DB was missing `pg_stat_statements`
for the same reason.

**Fix applied:** the source SQL for each was already kept as embedded
`.sql` resources under `Scripts/` (the same files the original migrations
read via `Assembly.GetManifestResourceStream` — see `AddFormVersionViewGen.cs`
in the pre-squash history for the pattern). A handful of functions
(`generate_scoresheets_view`, `generate_submissions_view`,
`generate_worksheets_view`, `get_next_sequence_number`) didn't have a
corresponding `Scripts/*.sql` file, so those were reconstructed via
`pg_dump --schema-only` against a real, fully-migrated database (the live
database is the ground truth for the *final* state of a function that was
redefined across several migrations — safer than hand-merging incremental
`CREATE OR REPLACE` edits from migration history). All of it is now
(re-)created at the end of `Initial.Up()` via `RunEmbeddedScript(...)` calls
and two `CREATE EXTENSION IF NOT EXISTS` statements, so a brand-new database
gets full parity.

Not restored: `public.populate_application_addresses()` — a one-time data-fix
helper function created by `AB29492_ApplicantAddress_Datafix`. It exists in
old databases only as a leftover from a completed one-time backfill; nothing
in the running application calls it, so it wasn't carried forward. Revisit
this call if that assumption turns out to be wrong.

**When redoing this squash**: after regenerating `Initial` via
`dotnet ef migrations add`, always diff `pg_proc`/`pg_extension` between a
real database and a fresh one that only ran the new `Initial` — a plain
schema-only `pg_dump` diff can miss this because functions/extensions aren't
part of the EF model either way; you have to check the actual database
objects, not just re-run the migration tool.

## Before doing this again: rebase first

Migrations keep landing on `dev` after a squash branch is cut. Regenerating
`Initial` from a stale branch point will silently drop any real schema/data
changes merged into `dev` afterward. **Rebase onto the latest `dev` (or
re-cut the branch) immediately before deleting migrations and regenerating
`Initial`**, not before.

## Things that make the *next* reset harder — avoid in new migrations

- **Raw data fixes mixed into schema migrations.** Permission renames
  (`RenameAIPermissions`, `ResetUnityPermission`, ...), config toggles
  (`AB30918_PaymentsEmailGroup_SetToStatic`), and bug-tied datafixes
  (`AB29460_ApplicantInfo_Datafix`, `AB33225_EmailSentDate_Datafix`, ...) are
  schema-diff-invisible: squashing to `Initial` permanently drops them from
  history. That's fine *only* because every live database had already run
  them before the squash — a database that lagged behind would silently miss
  a real bug fix. Prefer a small one-off seeder/data-migration service
  (see `TenantConnectionStringEncryptionMigrator`) for pure data corrections —
  it's self-describing, self-guarding, and doesn't get erased by a schema
  squash.
- **Broad, blanket data transformations** (`Renumber_UnityApplicationId_And_Seed_Counters`)
  are especially risky to carry through a reset — narrowly-scoped one-time
  fixes (`Backfill_UnityApplicationId_ABPP2025FallClaims`, scoped by form
  category) are much safer than ones that touch every tenant's data
  unconditionally. If a migration must reshape live data broadly, isolate it
  in its own migration (don't bundle with schema changes) so it's easy to
  spot and reason about before the next squash.
- **Reading external files at migration-run-time**
  (`RefreshBuiltInAIPromptVersions` reads `.txt` prompt assets via
  `AppContext.BaseDirectory`). This only works if those files still ship with
  the migrator build — fragile, and invisible when just reading the migration
  history list. Prefer seeding this kind of content at application startup
  instead of inside `Up()`.
- **`CREATE EXTENSION` and other privilege-sensitive DDL**
  (`InstallPgStatStatements`, `AddFuzzyStrMatchExtension`) require the
  migrating role to have elevated Postgres privileges. Confirm the prod
  connection actually has them before adding more of these — a squash just
  replays the same `CREATE EXTENSION IF NOT EXISTS`, so it's idempotent, but
  it will fail loudly on a role that isn't allowed to install extensions.
- **Snapshot drift.** This project's history includes several
  `FixSnapshot`/`FixSnapshotAgain*` migrations, meaning the `*DbContextModelSnapshot.cs`
  has gone out of sync with the real deployed schema more than once. Before
  trusting the snapshot as the basis for a squash, diff
  `dotnet ef migrations script` output (from empty) against a real
  `pg_dump --schema-only` of a live database. Consider adding a CI check that
  does this automatically so drift is caught immediately instead of
  accumulating for years.
