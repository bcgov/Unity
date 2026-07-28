# Evaluation dataset — Unity AI NL→SQL pipeline

A version-controlled benchmark of `question -> gold_sql -> expected result` triples
for the "Default Grants Program" tenant (db_id=3), used to check whether the
NL→SQL pipeline (`sql_generator.py` + Azure OpenAI + pgvector schema retrieval)
produces correct SQL over time.

The dataset ticket built the **dataset + its authoring/capture/verification
tooling**. The evaluation runner that exercises `sql_generator.generate_sql`
against this dataset now exists too — see `run_eval.py` under "Tooling" below.
CI wiring for `verify_dataset.py`/`run_eval.py` remains a follow-up.

## Current status

`eval/dataset/questions.jsonl` has **42 real, captured entries** authored and
verified against the live "Default Grants Program" test Metabase instance
(reachable from this environment — see `schema_reference/` for the schema
snapshot they were authored against). This is a first version, not a final
one: extending it further with more question shapes (and eventually
`question_type: "negative"/"ambiguous"` cases) is expected future work — see
"Extending the dataset" below.

All 6 `schema_type x difficulty` cells (public/worksheet-or-scoresheet ×
easy/medium/hard) have at least one entry, enforced by
`test_schema_type_x_difficulty_cells_covered` in `test_eval_dataset.py`:

| | easy | medium | hard |
|---|---|---|---|
| **public** | 10 | 10 | 2 |
| **worksheet/scoresheet** | 10 | 3 | 7 |

Public still has slightly more entries overall (22 vs. 20) since it's the
larger, more heavily-used schema, but worksheet/scoresheet coverage grew
substantially in a second authoring pass that deliberately targeted views
outside `Reporting.ReportColumnsMaps` (see the difficulty rubric notes below)
— it now exercises 11 of the 32 real worksheet/scoresheet views available on
this tenant, up from 5.

## Dataset entry schema (`eval/dataset/questions.jsonl`)

A pretty-printed JSON array, one object per entry (indented for readability;
all tooling reads/writes the whole file as a single JSON document via
`json.load`/`json.dump`, not line-by-line):

```json
{
  "id": "PUB-EASY-001",
  "question": "How many applications have been submitted in total?",
  "tenant_id": "Default Grants Program",
  "difficulty": "easy",
  "gold_sql": "SELECT COUNT(*) AS \"count\" FROM \"public\".\"Applications\";",
  "expected_columns": ["count"],
  "expected_column_types": ["type/BigInteger"],
  "expected_row_count": 1,
  "content_hash": "sha256:...",
  "frozen": false,
  "schema_type": "public",
  "tags": ["aggregation"],
  "question_type": "standard",
  "schema_version": "default_grants_program_schema_2026-07-08",
  "notes": "Sanity-check on total application volume; grows as new applications arrive on this shared test tenant.",
  "captured_at": "2026-07-06T20:53:00+00:00"
}
```

Fields are ordered by importance: the core `question -> gold_sql -> expected
result` triple first, then the verification flag (`frozen`), then
stratification/provenance metadata. Two fields notably **absent** on purpose
(dropped in favor of simplicity — see git history if you need the rationale):
`db_id` (100% derivable from `tenant_id` via `tenant_config.json`, so it isn't
stored per entry — tooling resolves it at run time via
`config.get_tenant_config(tenant_id)`) and `captured_by` (was a constant
`"capture_dataset.py"` on every entry, carrying no information).

Field notes:

- **`expected_columns`/`expected_column_types`/`expected_row_count`/
  `content_hash`** — a **content signature**, not raw rows: columns,
  column types (Metabase `base_type`, e.g. `type/BigInteger`), row count,
  and a `content_hash` (sha256 over columns + stringified row values). This
  keeps applicant/worksheet/scoresheet data out of a git-versioned file while
  still detecting drift.
- **`question_type`** — enum `"standard" | "negative" | "ambiguous"`, default
  `"standard"`. Reserves room for edge-case questions (unsupported question,
  insufficient info, needs clarification). Authoring `negative`/`ambiguous`
  entries is **not required** — a gold SQL + expected result per question is
  the acceptance bar, and negative cases don't have one by definition. Treat
  as a documented extension point for later, not part of the 30-50 count.
- **`schema_version`** — the `schema_reference/` snapshot filename/date the
  question was authored and validated against. Pins each entry to a known
  schema state, so a later schema change shows up as "entries older than
  schema_version X need re-validation" rather than silent drift.
- **`id`** — `<TIER>-<DIFFICULTY>-<seq>`, `TIER` ∈ `PUB` (public) / `WKS`
  (worksheet or scoresheet) — self-documents the stratification cell.
- **`schema_type`** — enum `"public" | "worksheet" | "scoresheet"` — collapses
  to the two required strata for reporting, but keeps worksheet vs scoresheet
  distinguishable (matches `embeddings.py`'s `=== WORKSHEET VIEWS ===` /
  `=== SCORESHEET VIEWS ===` sections).
- **`difficulty`** — enum `"easy" | "medium" | "hard"` — see the rubric below.
- **`gold_sql`** **must include an explicit `ORDER BY`** whenever more than
  one row can return — Postgres/Metabase gives no row-order guarantee
  without one, so `content_hash` wouldn't be reproducible across runs. Ties
  need a deterministic secondary sort key (see `PUB-MEDIUM-004`/`007`).
- **`frozen`** — `false` by default (live/growing data — drift is logged, not
  a failure, by `verify_dataset.py`). `true` only for questions anchored to
  closed/historical cohorts (all current worksheet/scoresheet entries qualify:
  they're small, stable QA fixtures), where `verify_dataset.py` treats
  `row_count`/`content_hash` mismatches as hard failures.

## Difficulty rubric (documented here, not enforced by code)

Difficulty is an axis **independent of `schema_type`** — a worksheet/scoresheet
question isn't automatically Hard just because it touches a custom view, and
a public-schema question isn't capped at Medium just because it's a core
table. Judge each gold SQL against these characteristics regardless of which
schema it queries:

| Difficulty | Characteristics |
|---|---|
| Easy | Single table, no join, simple filter/aggregation, no casting |
| Medium | 2-3 table joins, date filtering, and/or GROUP BY |
| Hard | Requires `::numeric`/`::date`-style casts on Text-typed columns, cross-view joins on `application_id` (worksheet/scoresheet), window functions, and/or nested subqueries |

This gives authors a concrete checklist instead of a subjective label. Two
notes from authoring against this schema:

- Worksheet/scoresheet view columns are *mostly* already typed correctly at
  the view level (Currency/Numeric/Date form fields map to Decimal/Date
  columns) — the genuine casting cases are `SelectList`-typed score fields
  stored as free text like `"(5 pts) Limited energy burden is demonstrated."`
  (see `WKS-HARD-002`/`003`, which use `regexp_replace` + `NULLIF` + `CAST` to
  pull the leading number out). A plain `COUNT`/`SUM` against a worksheet view
  with no cast and no join is Easy, same as it would be on a public table
  (see `WKS-EASY-001` through `007`).
- Conversely, a public-only query can be genuinely Hard: `PUB-HARD-001` ranks
  applications per intake with `ROW_NUMBER() OVER (PARTITION BY ...)` over a
  nested subquery, and `PUB-HARD-002` uses a correlated average inside a
  `HAVING` clause — neither touches a worksheet/scoresheet view.
- **Legacy (unregistered) worksheet/scoresheet views are a different story
  from the registered ones above**: every column comes through as raw
  `type/Text` — even fields that are clearly numeric (dollar amounts, years,
  scores) — because they were never registered in `ReportColumnsMaps` and so
  never got form-type-driven column coercion. `WKS-HARD-004` through `007`
  deliberately target these (e.g. `Worksheet-homeinsulationresiliencegrant-v1`,
  `Scoresheet-sri01scoresheet-v1`, `Scoresheet-cpscoresheet-v1`), including a
  direct mirror of `WKS-EASY-003`'s already-typed column (`WKS-HARD-004`) to
  make the contrast explicit. `WKS-HARD-006`'s label-parsing regex
  deliberately uses `substring(... from '\((\d+)\)')` rather than
  `regexp_replace(..., '\D', '', 'g')` — the latter would have grabbed a
  stray digit elsewhere in the text (`"(10) Option 1"` → `101` instead of
  `10`). `WKS-MEDIUM-003` and `WKS-HARD-007` also introduce join patterns not
  seen elsewhere in the dataset: joining a `Reporting`-schema view directly to
  a `public` table (`CorrelationId = Applications.Id`), and a 3-table bridge
  join through `Assessments` for the scoresheet twin, whose `CorrelationId`
  points at `Assessments.Id` rather than `Applications.Id` directly — verified
  live, not assumed from column naming.

## Coverage goals (enforced in `test_eval_dataset.py`)

Beyond the 30-50 total and the `schema_type x difficulty` 2x3 cross-product
(every cell non-empty, checked by `test_schema_type_x_difficulty_cells_covered`
— not just each axis's totals in isolation), the dataset requires at least 2
entries per tag so the benchmark exercises real query shapes, not just enough
rows to hit a count:

- `aggregation` (COUNT/SUM/AVG/etc.)
- `filter` (WHERE-only, no join)
- `join` (multi-table)
- `date` (date range/extraction logic)
- `worksheet` / `scoresheet` (cross-cutting queries tagged explicitly, beyond what `schema_type` already implies)
- `casting` (Text→numeric cast on worksheet/scoresheet columns)

## Tooling

All scripts live under `eval/` and do real network I/O against live Metabase
**except `csv_to_jsonl.py`/`sql_format.py`**, which are pure
offline transforms — this means
`python -m unittest discover` from the Backend root won't accidentally try to
hit the network, since none of these are named `test_*.py` except
`test_eval_dataset.py` (Backend root), which is itself offline-only.

### `sql_format.py` — whitespace-only `gold_sql` helper

Not a script — a helper used by `csv_to_jsonl.py` (`collapse_sql`,
`has_comment`). No dependencies, no network.

`collapse_sql` reduces a `gold_sql` cell to the canonical one-liner stored in
`questions.jsonl`, collapsing every whitespace run outside quoted strings to a
single space. It only ever rewrites whitespace — never inserts, drops, re-cases
or rewrites a character of SQL — so a `gold_sql` written across multiple lines
in the CSV for readability collapses back exactly, and re-wrapping a query is
never counted as an edit. `has_comment` rejects a cell containing a `--` or
`/* */` comment, which collapsing to one line would turn into a query-ending
comment.

### `csv_to_jsonl.py` — sync CSV rows back into JSONL (two-way, by `id`)

```
python eval/csv_to_jsonl.py eval/dataset/questions_template.csv eval/dataset/questions.jsonl
```

Pure offline transform, no network calls. For each CSV row:
- **`id` blank** — a brand new question. `id` is auto-generated per
  stratification cell (continuing from the highest existing sequence number),
  and the expected-result fields are left empty for `capture_dataset.py` to
  fill in.
- **`id` filled in and matches an existing entry** — updates that entry's
  editable fields (question, gold_sql, tags, notes, etc.) **in
  place**. If `gold_sql` changed from what's currently stored, the entry's
  expected-result fields are reset to the empty stub and `captured_at`
  cleared — otherwise an edited query would keep its old frozen answer and
  `verify_dataset.py` would silently compare against the wrong thing. Any
  other field edit (question wording, notes, tags, ...) does **not** touch
  the expected-result fields.
- **`id` filled in but not found** — hard error (typo, or a row for an entry
  someone already deleted from `questions.jsonl`).

`gold_sql` is collapsed back to one line before it's stored and compared, so
re-indenting or re-wrapping a query in the spreadsheet is **not** an edit and
won't trigger a re-capture. The flip side: SQL comments (`--`, `/* */`) in a
`gold_sql` cell are a hard error, since collapsing would comment out the rest
of the query — put the explanation in `notes`.

Deleting a question isn't supported via CSV — remove its line from
`questions.jsonl` directly (there's no "delete" marker column).

**Typical edit loop:** open `questions_template.csv`, edit/add rows, run
`csv_to_jsonl.py`, then run `capture_dataset.py` (no flags) — it automatically
picks up both brand-new questions and any updated one whose `gold_sql`
changed, since both now have empty expected-result fields.

### `capture_dataset.py` — execute gold SQL, fill in expected-result fields

```
python eval/capture_dataset.py                  # only entries missing a content_hash
python eval/capture_dataset.py --refresh WKS-HARD-004
python eval/capture_dataset.py --refresh-all
```

Resolves `db_id` from `tenant_id` via `config.get_tenant_config(tenant_id)`,
calls `metabase_client.execute_sql(gold_sql, db_id, tenant_id=tenant_id)`, and
fills `expected_columns`/`expected_column_types`/`expected_row_count`/
`content_hash`/`captured_at`. Requires live Metabase network access and a
`tenant_config.local.json` (or equivalent) `api_key` for the tenant.

### `verify_dataset.py` — re-run every gold SQL, check for drift

```
python eval/verify_dataset.py
```

Re-executes every `gold_sql`: checks it still runs and that returned
`columns`/`column_types` match what's stored (always a hard failure if not —
a schema rename should be caught immediately). For `frozen: true` entries,
`row_count`/`content_hash` mismatches are also hard failures. For
`frozen: false` entries, mismatches are logged as drift, not failures. Exits
non-zero on any hard failure — usable today as a manual check, and the
natural hook for a future CI job (not wired up here).

### `run_eval.py` — run the NL→SQL pipeline against the dataset and score it

```
python eval/run_eval.py                        # all captured entries
python eval/run_eval.py --ids PUB-EASY-001,WKS-HARD-004
python eval/run_eval.py --limit 5
python eval/run_eval.py --tenant "Default Grants Program"
python eval/run_eval.py --ids PUB-EASY-001 --runs 3   # consistency mode
```

For each entry: calls `sql_generator.generate_sql(question, [], db_id,
tenant_id=...)` directly (the semantic query cache lives only in api.py's
`/api/ask` route, so a direct call never reads or writes it — every run
measures raw generation; measured latency is therefore pure generation
latency, without the cache hits production users sometimes get), applies a
SELECT-only guard, executes the generated SQL and the gold SQL live
back-to-back via `run_query_checked` (so both see near-identical data state
— live gold is the scoring reference; the stored `content_hash` is only a
sanity cross-check for `frozen` entries, with a loud `[DRIFT-FROZEN]`
warning on mismatch), and scores:

- **Execution validity** — the generated SQL runs without error.
- **Execution accuracy (primary)** — *row-content match*: same column count
  and some column permutation of the generated rows gives the same multiset
  of stringified row-tuples as gold. Row order and column names/aliases are
  ignored; value pairing within rows is preserved. Deliberately strict at the
  string level (`"42"` ≠ `"42.0"`), matching `compute_content_hash`'s
  convention.
- **Execution accuracy (secondary, stricter)** — `content_hash_match`:
  recomputed hash of the generated result equals the hash of the live gold
  result (column names, column order, and row order all count).
- **Execution accuracy (secondary, looser)** — `row_match_canonical`:
  **OR semantics** — true when the primary rule passed, *or* when it failed
  on row content only and a retry with value canonicalization passes.
  Canonicalization collapses pure formatting differences symmetrically on
  both sides: `"42"` == `"42.0"` == `"4.2E+1"` (Decimal-normalized), and a
  midnight timestamp equals its date (`"2026-07-08T00:00:00"` ==
  `"2026-07-08"`). Nothing else (case, whitespace, timezone suffixes) is
  normalized. A canonical-ONLY pass — i.e. a pure formatting miss — is
  `row_match_canonical and not row_match`; the gap between
  `canonical_row_match_rate` and `row_match_rate` measures how much accuracy
  is lost to formatting alone. The primary rule and `content_hash` are
  unchanged.
- **Table selection** — `table_scoring`: the real tables/views referenced by
  the generated SQL vs the gold SQL, both parsed **on the fly** with
  `sqlglot` (Postgres dialect; nothing is stored in the dataset, so it can't
  go stale). Names are normalized to lowercase `schema.table` (unqualified
  names default to `public`, per Postgres search_path); CTE aliases are
  excluded. `match` is `exact` / `superset` / `subset` / `overlap` /
  `disjoint`; `missing`/`extra` list the differences, and the raw extracted
  lists are recorded so misparses can be audited by eye. Scoring runs even
  when the generated SQL failed to execute — that's the point: it separates
  retrieval failures ("wrong tables") from generation failures ("right
  tables, wrong SQL"). `table_selection_rate` = share of measured questions
  with `match == "exact"`, over those where both sides parsed. Known caveat:
  a generated unqualified `FROM x` normalizes to `public.x` and mismatches a
  gold `reporting.x` — defensible (that SQL wouldn't execute), and auditable
  via the raw lists. sqlglot is **optional**: `pip install -r
  eval/requirements.txt` (eval-only; the production image never installs
  it). Without it the run proceeds with a startup warning, `table_scoring`
  fields are null, and the rate prints as `-`.
- **Self-correction iterations** — distribution, mean, first-attempt success
  rate (from `metadata["self_correction"]`, default 1).
- **Latency** — wall-clock per `generate_sql` call; mean/p50/p95.
- **Token usage** — from the returned `token_usage`, summed across
  self-correction iterations.
- **Consistency (`--runs N`, default 1)** — with N>1, each question runs N
  consecutive attempts (each a full generate→execute→score cycle, gold
  re-executed adjacently every time, so disagreement measures model
  nondeterminism rather than data drift). The `questions` array stays flat —
  each record gains an `attempt` field — and the main aggregates pool all
  attempts (`n`/`unmeasured`/token totals then count attempts; *rates*
  remain comparable to single-run artifacts). A top-level `consistency`
  block (always present, `null` for `--runs 1`) reports per-question
  `row_match` outcome vectors, `consistent` flags, distinct generated-SQL
  counts (whitespace-normalized — a nondeterminism signal, not semantic
  equivalence), plus overall `flake_rate` / `consistent_rate` /
  `distinct_sql_mean`. Consistency is judged on the primary `row_match`
  only, over questions with ≥2 measured attempts. **Cost warning:** `--runs
  N` multiplies Azure token usage and wall time by N — a full 42-question
  pass is already ≈3.7M tokens and 10–30 minutes, so reserve large N for
  `--ids`/`--limit` subsets.

Aggregates are reported overall and by difficulty, schema_type,
difficulty×schema_type, and tag. Results go to a timestamped JSON artifact
under `eval/results/` (git-ignored — runs are frequent and may embed
generated SQL over tenant data; copy a run out deliberately to keep it) with
reproducibility metadata: `eval_schema_version` (now `"3"` — v2 added
per-question `row_match_canonical` / `canonical_match_reason` /
`table_scoring` / `attempt`, new aggregate rates, and top-level `runs` /
`consistency` / `sqlglot_version`; v3 adds combined multi-tenant runs and
dataset-set provenance), git commit,
dataset fingerprint, tenant list, and an AI-config snapshot (deployment,
k_samples, max iterations). A console summary table prints at the end.

The SELECT-only guard is an **intentional difference from production** (which
has no such guard — safety there rests on the read-only Metabase API key):
the runner executes untrusted generated SQL, so it fails closed. Guard
violations are their own category (`guard_violation`), never conflated with
generation/execution failures. `suspect_hardcoded` (flagged when a success
reports zero tokens) is a heuristic hint that the hardcoded-example shortcut
in `generate_sql` answered, not a definitive signal.

**Exit codes:** `0` clean measurement run (accuracy misses are data, not
failures); `1` runner/infrastructure failure — one or more questions
unmeasured (gold SQL failed live, runner error); `2` one or more
guard/safety violations (numbers still valid; an unsafe generation is a
model-behavior finding). `1` wins if both occur.

**Prerequisites:** live Metabase + Azure OpenAI **+ Postgres/pgvector**
(importing `sql_generator` connects to pgvector at import time — one more
requirement than the capture/verify scripts), plus a
`tenant_config.local.json` api_key. Optionally `sqlglot` for table-selection
scoring: `pip install -r eval/requirements.txt` (eval-only dependency — the
production Docker image installs only the root `requirements.txt` and never
copies `eval/`). Entries run **sequentially** on purpose:
each `generate_sql` call already fans out `k_samples` (default 7) concurrent
Azure requests on iteration 1, so parallel questions would multiply TPM
pressure into 429s and pollute latency numbers. Expect roughly 10–30 minutes
for the full dataset. Because gold runs live, absolute numbers can shift
between runs as `frozen: false` data grows — compare runs using the
artifact's metadata (dataset fingerprint, git commit, config snapshot).

The pure scoring functions (guard, row-content comparison, percentiles,
aggregation) are offline-unit-tested by `test_run_eval.py` at the Backend
root: `python -m unittest test_run_eval -v` — no live infra needed, since
`run_eval.py` defers all heavy imports into its runtime path.

### `test_eval_dataset.py` (Backend root) — offline structural checks

```
python -m unittest test_eval_dataset -v
```

No network calls. Validates: entry count is heading toward 30-50 (soft
warning below that for now); every entry has all required fields with correct
types, including `question_type`/`schema_version`;
`id`s are unique; `schema_type`/`difficulty`/`question_type` are valid enum
values; both schema strata and all three difficulties are represented; the
tag-coverage minimums above are met; `gold_sql` looks like a `SELECT` for
`standard` questions; `expected_columns` is non-empty for any entry
that's been captured.

## Extending the dataset toward 30-50+ entries

1. `questions_template.csv` is a full mirror of `questions.jsonl` — add new
   rows (leave `id` blank) and/or edit existing rows in place in the CSV.
2. `python eval/csv_to_jsonl.py eval/dataset/questions_template.csv eval/dataset/questions.jsonl` to sync the CSV back — new rows get new ids, edited rows update in place.
3. `python eval/capture_dataset.py` to fill in expected-result fields for anything new or changed.
4. `python eval/verify_dataset.py` to confirm they're stable.
5. `python -m unittest test_eval_dataset -v` before committing.

## Deferred to a follow-up ticket

`run_eval.py` now covers execution validity/accuracy (strict, hash, and
canonical variants), table-selection scoring, N-run consistency
measurement (`--runs`), self-correction iterations, latency, and token usage
(and records generated SQL + error detail per question). Still explicitly
deferred:

- SQL-quality/complexity/similarity scoring of generated vs. gold SQL
  (beyond the table-set comparison that now exists).
- True schema-retrieval-accuracy evaluation: `table_scoring` compares the
  tables the *generated SQL* references against gold — a downstream proxy.
  Comparing the actual pgvector-retrieved context (`embeddings.py`) against
  the gold SQL's tables remains deferred.
- Retrieved-schema-context logging on evaluation failure (the runner records
  generated SQL, execution result, and error detail, but not the pgvector
  context the prompt saw).
- Authoring and scoring actual `question_type: "negative"/"ambiguous"` content.
- CI integration of `verify_dataset.py` / `run_eval.py`.
- A bounded-concurrency mode for faster full runs.
- % row-overlap partial-credit scoring on content mismatches (deliberately
  excluded for now: most entries are single-row aggregates where it's
  meaningless).
