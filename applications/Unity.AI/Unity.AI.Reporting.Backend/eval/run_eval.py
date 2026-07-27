"""
Evaluation runner for the NL->SQL pipeline: exercises sql_generator.generate_sql
against eval/dataset/questions.jsonl and scores the generated SQL's live results
against a live re-run of each entry's gold_sql.

Metrics (per question and aggregated overall / by difficulty / by schema_type /
by difficulty x schema_type / by tag):
- execution validity (generated SQL runs without error)
- execution accuracy: row-content match (row order + column order/aliases
  ignored) as the primary rule, strict content-hash match as a stricter
  secondary number, and a looser canonical secondary (row_match OR
  match-after-value-canonicalization: '42' == '42.0', midnight timestamp ==
  date) that isolates pure formatting misses
- table selection: tables referenced by generated vs gold SQL (parsed on the
  fly with sqlglot when installed — see eval/requirements.txt; scoring is
  skipped gracefully otherwise)
- self-correction iterations (first-attempt success rate, distribution)
- latency (wall-clock per generate_sql call; mean/p50/p95)
- token usage
- with --runs N (N>1): cross-attempt consistency (flake rate, distinct
  generated SQL per question)

Requires live Metabase, Azure OpenAI, AND Postgres/pgvector (importing
sql_generator connects to pgvector at import time — one more prerequisite than
capture_dataset.py / verify_dataset.py), plus a tenant_config.local.json
api_key for each entry's tenant.

The semantic query cache lives only in api.py's /api/ask route; calling
generate_sql directly (as this runner does) never reads or writes it, so every
run measures raw generation. Note this also means measured latency is pure
generation latency — production users sometimes get faster cached responses.

Questions run sequentially on purpose: each generate_sql call already fans out
k_samples (default 7) concurrent Azure OpenAI requests on iteration 1, so
parallel questions would multiply TPM pressure into 429s (which abort a
question — iteration-1 infra errors re-raise) and pollute latency numbers
with queueing noise.

Exit codes:
    0 — clean measurement run (accuracy misses are data, not failures)
    1 — runner/infrastructure failure: one or more questions could not be
        measured (gold SQL failed to execute live, or the runner errored)
    2 — one or more guard/safety violations (the run's numbers are still
        valid; an unsafe generation is a model-behavior finding)
    If both occur, 1 wins.

There are two pipelines. The Default (synthetic) tenant keeps its single-file
run over questions.jsonl, unchanged. The production tenants run combined via
--prod-tenants, which loads the files named in dataset/prod_tenants.json and
routes each entry to its own db_id. Because the same id (e.g. PUB-EASY-001)
exists under multiple tenants, records are keyed by (tenant_id, id) throughout;
consistency.per_question is an array of {tenant_id, id, ...} objects.

Usage:
    # Default single-tenant pipeline (unchanged):
    python eval/run_eval.py                        # all captured Default entries
    python eval/run_eval.py --ids PUB-EASY-001,WKS-HARD-004
    python eval/run_eval.py --limit 5
    python eval/run_eval.py --output path.json
    python eval/run_eval.py --ids PUB-EASY-001 --runs 3   # consistency mode

    # Production multi-tenant pipeline (combined run over the manifest):
    python eval/run_eval.py --prod-tenants
    python eval/run_eval.py --prod-tenants --limit-per-tenant 2   # smoke: N per tenant
    python eval/run_eval.py --prod-tenants --ids AF-PSB:PUB-EASY-001,REDIP:PUB-EASY-001
    python eval/run_eval.py --prod-tenants --tenant REDIP
"""
import argparse
import asyncio
import hashlib
import json
import os
import re
import subprocess
import sys
import time
from collections import Counter
from datetime import datetime, timezone
from decimal import Decimal, InvalidOperation
from pathlib import Path
from types import SimpleNamespace
from typing import Dict, List, Optional, Tuple

DATASET_PATH = Path(__file__).parent / "dataset" / "questions.jsonl"
RESULTS_DIR = Path(__file__).parent / "results"

# Version of THIS artifact format — bump when the JSON schema below changes,
# so tooling comparing runs across time can detect incompatible artifacts.
# v2 = additive over v1: per-question row_match_canonical /
# canonical_match_reason / table_scoring / attempt; aggregate
# canonical_row_match_rate / table_selection_rate / table_parse_failures;
# top-level runs / consistency / sqlglot_version.
# v3 = multi-tenant combined runs: aggregates.by_tenant; consistency.per_question
# is now an ARRAY of {tenant_id, id, ...} objects (was an id-keyed map — tuple
# keys aren't JSON-serializable); singular dataset_path / dataset_fingerprint
# replaced by datasets[] (one descriptor per source file) + dataset_set_sha256.
EVAL_SCHEMA_VERSION = "3"

PERMUTATION_CAP = 5000

SHA256_PREFIX = "sha256:"

# ---------------------------------------------------------------------------
# Pure, import-safe functions (no unit tests committed — cover locally if you
# add any). Heavy imports (config / metabase / sql_generator, which need live
# Postgres) are deferred into _load_deps() so importing this module needs no
# infra.
# ---------------------------------------------------------------------------

# Line comments, block comments, string literals ('' escapes), and quoted
# identifiers ("" escapes) — removed before keyword scanning so a literal
# 'please delete me' or a column named "DeletedAt" can't false-positive.
_SQL_NOISE_RE = re.compile(
    r"--[^\n]*"
    r"|/\*.*?\*/"
    r"|'(?:[^']|'')*'"
    r'|"(?:[^"]|"")*"',
    re.DOTALL,
)

_FORBIDDEN_KEYWORD_RE = re.compile(
    r"\b(INSERT|UPDATE|DELETE|DROP|ALTER|TRUNCATE|CREATE|GRANT|REVOKE"
    r"|MERGE|COPY|CALL|DO|VACUUM|REINDEX|LOCK|SET|RESET|COMMENT|INTO)\b",
    re.IGNORECASE,
)


def strip_strings_and_comments(sql: str) -> str:
    """Replace comments, string literals, and quoted identifiers with a space."""
    return _SQL_NOISE_RE.sub(" ", sql)


def check_select_only(sql: str) -> Optional[str]:
    """Return a violation reason if the SQL is not a single read-only
    SELECT/WITH statement, else None.

    Intentional difference from production, which has no guard at all (safety
    there rests on the read-only Metabase API key): this runner executes
    untrusted generated SQL, so it fails closed. Scanning for forbidden
    keywords ANYWHERE (not just the leading word) also catches Postgres
    data-modifying CTEs like `WITH d AS (DELETE ... RETURNING ...) SELECT ...`.
    """
    stripped = strip_strings_and_comments(sql).strip()
    if not stripped:
        return "empty statement"
    if not re.match(r"(?i)(SELECT|WITH)\b", stripped):
        return f"statement does not start with SELECT/WITH: {stripped.split(None, 1)[0]!r}"
    if ";" in stripped.rstrip("; \t\r\n"):
        return "multiple statements (embedded ';')"
    m = _FORBIDDEN_KEYWORD_RE.search(stripped)
    if m:
        return f"forbidden keyword {m.group(1).upper()!r}"
    return None


def stringify_rows(rows: List[list], value_fn=None) -> List[Tuple[str, ...]]:
    """str() every value — same convention as capture_dataset.compute_content_hash,
    so NULL->'None', Decimal/datetime formatting etc. are symmetric on both sides.
    Deliberately strict: '1' != '1.0' and '2026-07-08' != '2026-07-08T00:00:00'.
    value_fn, if given, post-processes each stringified cell (see
    canonicalize_value) — applied to gold and generated alike."""
    if value_fn is None:
        return [tuple(str(v) for v in row) for row in rows]
    return [tuple(value_fn(str(v)) for v in row) for row in rows]


# Date part of a timestamp whose time is exactly midnight (optionally with a
# fractional .000...) — the only datetime shape canonicalize_value collapses.
_MIDNIGHT_RE = re.compile(r"^(\d{4}-\d{2}-\d{2})[T ]00:00:00(?:\.0+)?$")


def canonicalize_value(s: str) -> str:
    """Normalize the known cosmetic formatting differences between equal
    values: '42' == '42.0' == '4.2E+1', and '2026-07-08T00:00:00' ==
    '2026-07-08'. Everything else (case, whitespace, 'None', 'NaN') is left
    untouched. Timezone suffixes are deliberately not handled: both sides of
    every comparison come through the same Metabase-JSON -> str() path, so tz
    representation is already symmetric."""
    if s != s.strip():
        # Decimal() tolerates surrounding whitespace; keep it significant.
        return s
    m = _MIDNIGHT_RE.match(s)
    if m:
        return m.group(1)
    try:
        d = Decimal(s)
    except InvalidOperation:
        return s
    if not d.is_finite():
        return s  # 'NaN'/'Infinity' parse but must not compare equal
    if d == 0:
        return "0"  # collapses '0', '0.0', '-0', '0E-2'
    # format(..., 'f') is load-bearing: Decimal('420').normalize() is 4.2E+2,
    # so plain str() would turn '420' into '4.2E+2'.
    return format(d.normalize(), "f")


class _PermutationCapExceeded(Exception):
    pass


def rows_match(gold_rows: List[list], gen_rows: List[list],
               gold_ncols: int, gen_ncols: int,
               permutation_cap: int = PERMUTATION_CAP,
               value_fn=None) -> Tuple[bool, str]:
    """Primary accuracy rule: permutation row-multiset match.

    Match = same column count AND some column permutation of the generated
    rows yields the same multiset of stringified row-tuples as gold. Row
    order and column names/aliases are ignored; row-wise value pairing is
    preserved exactly (unlike sorting values within each row, which would
    false-positive on cross-column transpositions).

    value_fn (e.g. canonicalize_value) post-processes every stringified cell
    on BOTH sides before any comparison — including the per-column multisets
    that prune the permutation search.

    Returns (matched, reason) — reason is "exact" / "column_permutation" on
    match, or "column_count_mismatch" / "row_count_mismatch" /
    "row_content_mismatch" / "permutation_search_exhausted" on miss.
    """
    if gold_ncols != gen_ncols:
        return False, "column_count_mismatch"
    gold_s = stringify_rows(gold_rows, value_fn)
    gen_s = stringify_rows(gen_rows, value_fn)
    if len(gold_s) != len(gen_s):
        return False, "row_count_mismatch"
    if not gold_s:
        return True, "exact"

    gold_counter = Counter(gold_s)
    if gold_counter == Counter(gen_s):
        return True, "exact"

    n = gold_ncols
    gold_col_multisets = [Counter(r[i] for r in gold_s) for i in range(n)]
    gen_col_multisets = [Counter(r[j] for r in gen_s) for j in range(n)]
    # Generated column j can only stand in for gold position i if their full
    # per-column value multisets agree — prunes the search to the handful of
    # genuinely ambiguous permutations.
    candidates = [
        [j for j in range(n) if gen_col_multisets[j] == gold_col_multisets[i]]
        for i in range(n)
    ]
    if any(not c for c in candidates):
        return False, "row_content_mismatch"

    order = sorted(range(n), key=lambda i: len(candidates[i]))
    tried = 0

    def backtrack(pos: int, used: set, perm: Dict[int, int]) -> bool:
        nonlocal tried
        if pos == n:
            tried += 1
            if tried > permutation_cap:
                raise _PermutationCapExceeded()
            permuted = Counter(tuple(r[perm[i]] for i in range(n)) for r in gen_s)
            return permuted == gold_counter
        i = order[pos]
        for j in candidates[i]:
            if j in used:
                continue
            used.add(j)
            perm[i] = j
            if backtrack(pos + 1, used, perm):
                return True
            used.discard(j)
        return False

    try:
        if backtrack(0, set(), {}):
            return True, "column_permutation"
    except _PermutationCapExceeded:
        return False, "permutation_search_exhausted"
    return False, "row_content_mismatch"


def extract_tables(sql: str) -> Tuple[Optional[List[str]], Optional[str]]:
    """Parse SQL and return (sorted normalized 'schema.table' names, error_note).

    Normalization: names lowercased (quoted-vs-unquoted case matters for
    execution, not for judging table selection; collision risk in this schema
    is nil), unqualified names default to 'public' (Postgres search_path).
    Unqualified references shadowed by a CTE alias are excluded;
    schema-qualified ones survive. sqlglot is imported lazily so run_eval
    stays import-light and works (with table scoring disabled) when the
    optional eval dependency isn't installed:
    (None, 'sqlglot_not_installed') on ImportError,
    (None, 'parse_error: ...') when parsing fails."""
    try:
        import sqlglot
        from sqlglot import exp
    except ImportError:
        return None, "sqlglot_not_installed"
    try:
        parsed = sqlglot.parse_one(sql, read="postgres")
    except Exception as e:
        return None, f"parse_error: {e!r}"[:300]
    cte_names = {c.alias_or_name.lower() for c in parsed.find_all(exp.CTE)}
    tables = set()
    for t in parsed.find_all(exp.Table):
        name = t.name.lower()
        if not t.db and name in cte_names:
            continue  # reference to a CTE, not a real table
        tables.add(f"{(t.db or 'public').lower()}.{name}")
    return sorted(tables), None


def classify_table_overlap(gold: set, gen: set) -> str:
    """How the generated SQL's table set relates to gold's:
    'exact' / 'superset' (gen ⊃ gold) / 'subset' (gen ⊂ gold) /
    'overlap' (partial intersection) / 'disjoint'."""
    if gen == gold:
        return "exact"
    if gen > gold:
        return "superset"
    if gen < gold:
        return "subset"
    if gen & gold:
        return "overlap"
    return "disjoint"


def percentile(values: List[float], p: float) -> Optional[float]:
    """Nearest-rank percentile; None on empty input."""
    if not values:
        return None
    ordered = sorted(values)
    k = max(0, -(-len(ordered) * p // 100) - 1)  # ceil(len*p/100) - 1
    return ordered[int(min(k, len(ordered) - 1))]


def _rate(num: int, den: int) -> Optional[float]:
    return round(num / den, 4) if den else None


def _aggregate(records: List[dict]) -> dict:
    n = len(records)
    measured = [r for r in records if r.get("measured")]
    m = len(measured)
    successes = [r for r in measured if r.get("row_match")]

    iterations = [r["self_correction_iterations"] for r in records
                  if r.get("self_correction_iterations") is not None]
    distribution = {str(k): v for k, v in sorted(Counter(iterations).items())}
    failed_generations = sum(
        1 for r in records if r.get("self_correction_iterations") is None
    )
    if failed_generations:
        distribution["failed"] = failed_generations

    latencies = [r["latency_ms"] for r in records if r.get("latency_ms") is not None]
    token_totals = [r["tokens"]["total_tokens"] for r in records
                    if r.get("tokens") and "total_tokens" in r["tokens"]]

    # Table-selection is only scorable where both gold and generated SQL parsed.
    table_scored = [r for r in measured
                    if (r.get("table_scoring") or {}).get("match") is not None]

    return {
        "n": n,
        "unmeasured": n - m,
        "safety_violations": sum(1 for r in records if r.get("guard_violation")),
        "validity_rate": _rate(sum(1 for r in measured if r.get("valid_execution")), m),
        "row_match_rate": _rate(len(successes), m),
        "canonical_row_match_rate": _rate(
            sum(1 for r in measured if r.get("row_match_canonical")), m),
        "hash_match_rate": _rate(sum(1 for r in measured if r.get("content_hash_match")), m),
        "table_selection_rate": _rate(
            sum(1 for r in table_scored if r["table_scoring"]["match"] == "exact"),
            len(table_scored)),
        "table_parse_failures": sum(
            1 for r in measured if (r.get("table_scoring") or {}).get("note")),
        "first_attempt_success_rate": _rate(
            sum(1 for r in successes if r.get("self_correction_iterations") == 1),
            len(successes),
        ),
        "iterations": {
            "mean": round(sum(iterations) / len(iterations), 2) if iterations else None,
            "distribution": distribution,
        },
        "latency_ms": {
            "mean": int(sum(latencies) / len(latencies)) if latencies else None,
            "p50": percentile(latencies, 50),
            "p95": percentile(latencies, 95),
        },
        "tokens": {
            "total": sum(token_totals),
            "mean_per_question": int(sum(token_totals) / len(token_totals)) if token_totals else None,
        },
    }


def build_aggregates(records: List[dict]) -> dict:
    def strata(key_fn) -> dict:
        groups: Dict[str, List[dict]] = {}
        for r in records:
            for key in key_fn(r):
                groups.setdefault(key, []).append(r)
        return {k: _aggregate(v) for k, v in sorted(groups.items())}

    return {
        "overall": _aggregate(records),
        "by_tenant": strata(lambda r: [r["tenant_id"]]),
        "by_difficulty": strata(lambda r: [r["difficulty"]]),
        "by_schema_type": strata(lambda r: [r["schema_type"]]),
        "by_difficulty_schema": strata(lambda r: [f"{r['difficulty']}/{r['schema_type']}"]),
        "by_tag": strata(lambda r: r.get("tags") or []),
    }


def build_consistency(records: List[dict], runs: int) -> Optional[dict]:
    """Cross-attempt consistency report for --runs N (None when runs == 1).

    Consistency is judged on the primary row_match only, over measured
    attempts. distinct_generated_sql collapses whitespace before comparing —
    a cheap nondeterminism signal, not semantic SQL equivalence."""
    if runs == 1:
        return None

    # Key by (tenant_id, id): in a combined prod run the same id (e.g.
    # PUB-EASY-001) exists under multiple tenants, so keying on bare id would
    # merge unrelated questions. Tuples are dict keys internally only —
    # per_question is emitted as an ARRAY of objects (tuple keys aren't
    # JSON-serializable; see EVAL_SCHEMA_VERSION v3).
    by_key: Dict[Tuple[str, str], List[dict]] = {}
    for r in records:
        by_key.setdefault((r["tenant_id"], r["id"]), []).append(r)

    per_question = []
    eligible = 0
    flaky = []  # list of {tenant_id, id}
    distinct_counts = []
    for (tenant_id, qid), attempts in sorted(by_key.items()):
        attempts = sorted(attempts, key=lambda r: r.get("attempt", 1))
        measured = [r for r in attempts if r.get("measured")]
        outcomes = [bool(r.get("row_match")) for r in measured]
        sqls = {" ".join(r["generated_sql"].split())
                for r in attempts if r.get("generated_sql")}
        consistent = len(set(outcomes)) <= 1 if len(outcomes) >= 2 else None
        if consistent is not None:
            eligible += 1
            if not consistent:
                flaky.append({"tenant_id": tenant_id, "id": qid})
        distinct_counts.append(len(sqls))
        per_question.append({
            "tenant_id": tenant_id,
            "id": qid,
            "attempts": len(attempts),
            "measured_attempts": len(measured),
            "row_match_outcomes": outcomes,
            "consistent": consistent,
            "distinct_generated_sql": len(sqls),
            "generation_failures": sum(
                1 for r in attempts if r.get("generated_sql") is None),
            "row_match_rate": _rate(sum(outcomes), len(outcomes)),
        })

    return {
        "runs": runs,
        "eligible_questions": eligible,
        "flaky_questions": flaky,
        "flake_rate": _rate(len(flaky), eligible),
        "consistent_rate": _rate(eligible - len(flaky), eligible),
        "distinct_sql_mean": (
            round(sum(distinct_counts) / len(distinct_counts), 2)
            if distinct_counts else None),
        "per_question": per_question,
    }


# ---------------------------------------------------------------------------
# Multi-tenant dataset discovery / validation / selection / provenance.
# Pure and infra-free (they take already-parsed data), so test_run_eval.py can
# unit-test them without the live stack — these are the load-bearing additions
# for combined prod runs, not just build_aggregates().
# ---------------------------------------------------------------------------

# Prod-tenant suite manifest (checked-in), read by --prod-tenants. An explicit
# list, deliberately NOT an auto-glob of questions_*.jsonl: a stray/WIP file
# must never silently join an expensive multi-tenant run.
MANIFEST_PATH = Path(__file__).parent / "dataset" / "prod_tenants.json"


def resolve_manifest_paths(manifest_data: dict, base_dir: Path) -> List[Path]:
    """Turn a parsed manifest ({"datasets": [names...]}) into sorted absolute
    paths, resolved relative to the manifest's own directory. Sorted so the
    load order — and therefore dataset_set_sha256 — is deterministic."""
    names = manifest_data.get("datasets")
    if not isinstance(names, list) or not names:
        raise ValueError("manifest has no non-empty 'datasets' list")
    return sorted((base_dir / str(n)).resolve() for n in names)


def normalize_tenant_token(tenant_id: str) -> str:
    """Filename token for a tenant_id: 'AF-PSB' -> 'af-psb'. The per-tenant
    file for tenant T is expected to be named questions_<token>.jsonl."""
    return tenant_id.lower()


def validate_prod_file(path: Path, entries: List[dict], default_tenant: str) -> List[str]:
    """Structural invariants a per-tenant prod file must satisfy. Returns a
    list of human-readable problems (empty == clean):
      - non-empty
      - every entry shares one tenant_id (a file is one tenant)
      - that tenant is not the Default (synthetic) tenant
      - filename matches questions_<normalized-tenant>.jsonl
    """
    problems = []
    if not entries:
        problems.append(f"{path.name}: file is empty")
        return problems
    tenant_ids_raw = {e.get("tenant_id") for e in entries}
    tenant_ids = sorted(t for t in tenant_ids_raw if t is not None)
    if len(tenant_ids_raw) != 1 or not tenant_ids:
        problems.append(
            f"{path.name}: expected exactly one tenant, found {sorted(str(t) for t in tenant_ids_raw)}")
    tenant_id = tenant_ids[0] if len(tenant_ids) == 1 else None
    if tenant_id == default_tenant:
        problems.append(
            f"{path.name}: contains Default tenant '{default_tenant}' entries — "
            "the Default tenant uses its own single-tenant run, not --prod-tenants")
    if tenant_id is not None:
        expected = f"questions_{normalize_tenant_token(tenant_id)}.jsonl"
        if path.name != expected:
            problems.append(
                f"{path.name}: filename does not match tenant '{tenant_id}' "
                f"(expected {expected})")
    return problems


def check_global_id_uniqueness(entries: List[dict]) -> List[str]:
    """Every (tenant_id, id) across the combined set must be unique. Returns a
    list of duplicate '(tenant, id)' descriptions (empty == clean)."""
    seen = Counter((e.get("tenant_id"), e.get("id")) for e in entries)
    return [f"({t}, {i})" for (t, i), n in sorted(seen.items()) if n > 1]


def parse_selectors(ids_arg: str) -> List[Tuple[Optional[str], str]]:
    """Parse --ids into (tenant_id_or_None, id) selectors. Each comma-separated
    token is either 'tenant:id' (qualified) or 'id' (bare). Whitespace tolerated."""
    selectors = []
    for tok in ids_arg.split(","):
        tok = tok.strip()
        if not tok:
            continue
        if ":" in tok:
            tenant, _, qid = tok.partition(":")
            selectors.append((tenant.strip(), qid.strip()))
        else:
            selectors.append((None, tok))
    return selectors


def _match_one_selector(entries: List[dict], tenant: Optional[str], qid: str
                        ) -> Tuple[List[dict], Optional[str]]:
    """Resolve a single selector to matching entries, or an error string.
    Bare id is ambiguous (errors) when it hits more than one tenant."""
    if tenant is not None:
        matches = [e for e in entries
                   if e["tenant_id"] == tenant and e["id"] == qid]
        label = f"{tenant}:{qid}"
    else:
        matches = [e for e in entries if e["id"] == qid]
        tenants_hit = {e["tenant_id"] for e in matches}
        if len(tenants_hit) > 1:
            return [], (f"id '{qid}' is ambiguous across tenants "
                        f"{sorted(tenants_hit)} — qualify it as tenant:id")
        label = qid
    if not matches:
        return [], f"selector {label}: no match"
    return matches, None


def apply_selectors(entries: List[dict],
                    selectors: List[Tuple[Optional[str], str]]
                    ) -> Tuple[List[dict], List[str]]:
    """Filter entries by --ids selectors. Returns (selected, errors).

    A qualified selector 'tenant:id' matches exactly that entry. A bare 'id'
    matches only when unique across the current pool — if it hits >1 tenant it
    is ambiguous and errors, telling the user to qualify it. Unmatched
    selectors also error, so a typo never silently drops a question."""
    errors = []
    selected = []
    taken = set()  # (tenant_id, id) already chosen — keeps output dedup'd
    for tenant, qid in selectors:
        matches, error = _match_one_selector(entries, tenant, qid)
        if error:
            errors.append(error)
            continue
        for e in matches:
            key = (e["tenant_id"], e["id"])
            if key not in taken:
                taken.add(key)
                selected.append(e)
    return selected, errors


def limit_per_tenant(entries: List[dict], n: int) -> List[dict]:
    """First n entries of each tenant, preserving input order. Unlike a global
    slice, this guarantees every tenant is represented in a combined run."""
    counts: Dict[str, int] = {}
    out = []
    for e in entries:
        t = e["tenant_id"]
        if counts.get(t, 0) < n:
            counts[t] = counts.get(t, 0) + 1
            out.append(e)
    return out


def file_sha256(path: Path) -> str:
    return SHA256_PREFIX + hashlib.sha256(path.read_bytes()).hexdigest()


def dataset_descriptor(path: Path, entries: List[dict]) -> dict:
    """Per-file provenance for the artifact's datasets[] list."""
    return {
        "path": str(path),
        "sha256": file_sha256(path),
        "entry_count": len(entries),
        "tenant_ids": sorted({e.get("tenant_id") for e in entries}),
        "schema_versions": sorted({e.get("schema_version", "") for e in entries}),
    }


def dataset_set_sha256(descriptors: List[dict]) -> str:
    """Single hash proving which files (by content) produced a combined run —
    the sha256 of the sorted per-file sha256s."""
    joined = "\n".join(sorted(d["sha256"] for d in descriptors))
    return SHA256_PREFIX + hashlib.sha256(joined.encode("utf-8")).hexdigest()


# ---------------------------------------------------------------------------
# Runtime — everything below needs the live stack.
# ---------------------------------------------------------------------------

def _light_imports() -> SimpleNamespace:
    """Import only the infra-free pieces needed for dataset discovery,
    validation, and selection — config (reads tenant_config.json, no DB) and
    capture_dataset.load_entries. Deliberately does NOT import sql_generator
    (which connects pgvector at import) or chdir, so all of main()'s argument
    validation and fail-fast checks run before the heavy stack is touched."""
    src_dir = Path(__file__).parent.parent / "src"
    sys.path.insert(0, str(src_dir))
    sys.path.insert(0, str(Path(__file__).parent))
    from config import config, DEFAULT_TENANT  # noqa: E402
    from capture_dataset import load_entries  # noqa: E402
    return SimpleNamespace(
        config=config,
        default_tenant=DEFAULT_TENANT,
        load_entries=load_entries,
    )


def preflight_embeddings(sql_generator, db_ids: List[int]) -> List[Tuple[int, str]]:
    """Confirm pgvector actually has schema rows for every selected db_id.

    Needed because generate_sql returns four Nones with no error detail when
    retrieval finds nothing (sql_generator.py:428), so the runner otherwise
    can't distinguish 'embeddings never generated' from a real generation
    failure. Returns (db_id, reason) for each db_id that is not ready."""
    missing = []
    for db_id in sorted(set(db_ids)):
        try:
            hits = sql_generator.embeddings.vector_store.similarity_search(
                "table", k=1, filter={"db_id": db_id, "schema_type": "public"})
        except Exception as e:
            missing.append((db_id, f"vector-store query failed: {e!r}"))
            continue
        if not hits:
            missing.append((db_id, "no public schema rows — run `python app.py embed-all`"))
    return missing


def _load_deps() -> SimpleNamespace:
    """Import the live-stack modules. Importing sql_generator pulls in
    embeddings.py, which connects to Postgres/pgvector at import time."""
    src_dir = Path(__file__).parent.parent / "src"
    sys.path.insert(0, str(src_dir))
    sys.path.insert(0, str(Path(__file__).parent))
    from config import config  # noqa: E402
    from metabase import metabase_client  # noqa: E402
    from sql_generator import sql_generator  # noqa: E402
    from capture_dataset import compute_content_hash, load_entries  # noqa: E402
    # Match production's working directory: sql_generator opens
    # QDECOMP_examples.json relative to cwd, and the app runs from src/.
    # Without this, an eval run silently drops the few-shot examples and
    # measures a degraded pipeline. (All of this script's own paths are
    # absolute via Path(__file__), so the chdir is safe.)
    os.chdir(src_dir)
    return SimpleNamespace(
        config=config,
        metabase_client=metabase_client,
        sql_generator=sql_generator,
        compute_content_hash=compute_content_hash,
        load_entries=load_entries,
    )


def _sqlglot_version() -> Optional[str]:
    """Provenance for table-selection scoring; None when not installed."""
    try:
        import sqlglot
        return sqlglot.__version__
    except Exception:
        return None


def _git_commit() -> Optional[str]:
    try:
        out = subprocess.run(
            ["git", "rev-parse", "HEAD"],
            capture_output=True, text=True, timeout=10,
            cwd=str(Path(__file__).parent),
        )
        return out.stdout.strip() if out.returncode == 0 else None
    except Exception:
        return None


def run_one(entry: dict, deps: SimpleNamespace) -> dict:
    record = {
        "id": entry["id"],
        "question": entry["question"],
        "tenant_id": entry["tenant_id"],
        "schema_type": entry["schema_type"],
        "difficulty": entry["difficulty"],
        "tags": entry.get("tags", []),
        "frozen": entry.get("frozen", False),
        "generated_sql": None,
        "generation_error": None,
        "guard_violation": None,
        "valid_execution": False,
        "execution_error": None,
        "row_match": False,
        "row_match_reason": None,
        "row_match_canonical": False,
        "canonical_match_reason": None,
        "content_hash_match": False,
        "table_scoring": None,
        "gold_live": None,
        "generated_shape": None,
        "self_correction_iterations": None,
        "latency_ms": None,
        "tokens": None,
        "suspect_hardcoded": False,
        "measured": False,
    }
    tenant_id = entry["tenant_id"]
    db_id = deps.config.get_tenant_config(tenant_id)["db_id"]

    # 1. Generate. One asyncio.run per call, matching api.py — generate_sql
    # builds its httpx client per request and expects its own event loop.
    # Iteration-1 infra errors (429/timeout) re-raise by design, so catch here.
    t0 = time.monotonic()
    sql = metadata = token_usage = None
    try:
        sql, metadata, token_usage, error_detail = asyncio.run(
            deps.sql_generator.generate_sql(entry["question"], [], db_id, tenant_id=tenant_id)
        )
        if sql is None:
            record["generation_error"] = error_detail or "no SQL generated (schema retrieval or relevance check failed)"
    except Exception as e:
        record["generation_error"] = repr(e)
    record["latency_ms"] = int((time.monotonic() - t0) * 1000)
    record["generated_sql"] = sql
    record["tokens"] = token_usage
    if sql is not None:
        record["self_correction_iterations"] = (
            (metadata or {}).get("self_correction", {}).get("iterations", 1)
        )
        # Heuristic, not a definitive signal: the hardcoded-example shortcut in
        # generate_sql returns zero tokens; SDK/model reporting changes could
        # also produce zero. Treat as an investigation hint.
        record["suspect_hardcoded"] = bool(token_usage) and token_usage.get("total_tokens", 1) == 0

        # 2. SELECT-only guard — on violation, never execute the SQL.
        record["guard_violation"] = check_select_only(sql)

    # 2.5. Table-selection scoring — parsing is read-only, so this runs even
    # for guard-violating or non-executing SQL (that's the diagnostic point:
    # "right tables, wrong SQL" vs "wrong tables").
    gold_tables, gold_note = extract_tables(entry["gold_sql"])
    gen_tables = gen_note = None
    if sql is not None:
        gen_tables, gen_note = extract_tables(sql)
    tbl_match = tbl_missing = tbl_extra = None
    if gold_tables is not None and gen_tables is not None:
        tbl_match = classify_table_overlap(set(gold_tables), set(gen_tables))
        tbl_missing = sorted(set(gold_tables) - set(gen_tables))
        tbl_extra = sorted(set(gen_tables) - set(gold_tables))
    record["table_scoring"] = {
        "gold_tables": gold_tables,
        "generated_tables": gen_tables,
        "match": tbl_match,
        "missing": tbl_missing,
        "extra": tbl_extra,
        "note": "; ".join(p for p in (
            f"gold: {gold_note}" if gold_note else None,
            f"generated: {gen_note}" if gen_note else None) if p) or None,
    }

    # 3. Execute generated SQL (skipped on generation failure / guard violation).
    gen_cols = gen_rows = None
    if sql is not None and record["guard_violation"] is None:
        try:
            is_valid, error, data = deps.metabase_client.run_query_checked(
                sql, db_id, tenant_id=tenant_id
            )
        except Exception as e:
            is_valid, error, data = False, repr(e), None
        record["valid_execution"] = is_valid
        record["execution_error"] = error
        if is_valid:
            gen_cols = data.get("cols", [])
            gen_rows = data.get("rows", [])
            record["generated_shape"] = {
                "row_count": len(gen_rows),
                "columns": [c["name"] for c in gen_cols],
            }

    # 4. Execute gold SQL live, immediately adjacent, so gold and generated
    # results reflect near-identical data state (drift window = seconds).
    # Live gold is the scoring reference; the stored hash is only a sanity
    # cross-check for frozen entries.
    try:
        gold_valid, gold_error, gold_data = deps.metabase_client.run_query_checked(
            entry["gold_sql"], db_id, tenant_id=tenant_id
        )
    except Exception as e:
        gold_valid, gold_error, gold_data = False, repr(e), None
    if not gold_valid:
        record["gold_live"] = {"error": gold_error}
        return record  # measured stays False -> unmeasured, exit code 1

    gold_cols = gold_data.get("cols", [])
    gold_rows = gold_data.get("rows", [])
    gold_live_hash = deps.compute_content_hash(gold_cols, gold_rows)
    matches_stored = (gold_live_hash == entry.get("content_hash")) if entry.get("frozen") else None
    record["gold_live"] = {
        "row_count": len(gold_rows),
        "content_hash": gold_live_hash,
        "matches_stored_hash": matches_stored,
    }
    if matches_stored is False:
        print(f"  [DRIFT-FROZEN] {entry['tenant_id']}:{entry['id']}: frozen entry's "
              f"live gold result differs from stored content_hash — run verify_dataset.py")
    record["measured"] = True

    # 5. Compare.
    if gen_cols is not None:
        matched, reason = rows_match(gold_rows, gen_rows, len(gold_cols), len(gen_cols))
        record["row_match"] = matched
        record["row_match_reason"] = reason
        # Secondary, looser rule: retry with value canonicalization when the
        # only difference can be value formatting ('42' vs '42.0'). OR
        # semantics: row_match_canonical is True for every primary pass too —
        # a canonical-ONLY pass is (row_match_canonical and not row_match).
        # permutation_search_exhausted is not retried: canonicalization merges
        # values, which only widens the permutation search.
        canonical = matched
        if not matched and reason == "row_content_mismatch":
            canonical, canon_reason = rows_match(
                gold_rows, gen_rows, len(gold_cols), len(gen_cols),
                value_fn=canonicalize_value)
            record["canonical_match_reason"] = canon_reason
        record["row_match_canonical"] = canonical
        record["content_hash_match"] = (
            deps.compute_content_hash(gen_cols, gen_rows) == gold_live_hash
        )
    return record


def _print_summary(aggregates: dict):
    def fmt_rate(v):
        return f"{v:.2f}" if v is not None else "-"

    def fmt(v):
        return str(v) if v is not None else "-"

    rows = [("overall", aggregates["overall"])]
    rows += [(f"tenant:{k}", v) for k, v in aggregates["by_tenant"].items()]
    rows += [(k, v) for k, v in aggregates["by_difficulty"].items()]
    rows += [(k, v) for k, v in aggregates["by_schema_type"].items()]

    print("\n=== Eval summary ===")
    header = (f"{'stratum':<22}{'n':>4}{'valid':>7}{'row_match':>11}{'canon':>7}"
              f"{'hash_match':>12}{'tables':>8}{'iters':>7}{'p50ms':>9}{'p95ms':>9}{'tok/q':>8}")
    print(header)
    print("-" * len(header))
    for name, a in rows:
        print(f"{name:<22}{a['n']:>4}{fmt_rate(a['validity_rate']):>7}"
              f"{fmt_rate(a['row_match_rate']):>11}{fmt_rate(a['canonical_row_match_rate']):>7}"
              f"{fmt_rate(a['hash_match_rate']):>12}{fmt_rate(a['table_selection_rate']):>8}"
              f"{fmt(a['iterations']['mean']):>7}{fmt(a['latency_ms']['p50']):>9}"
              f"{fmt(a['latency_ms']['p95']):>9}{fmt(a['tokens']['mean_per_question']):>8}")
    overall = aggregates["overall"]
    print(f"\nunmeasured={overall['unmeasured']} safety_violations={overall['safety_violations']} "
          f"first_attempt_success_rate={fmt_rate(overall['first_attempt_success_rate'])} "
          f"table_parse_failures={overall['table_parse_failures']} "
          f"total_tokens={overall['tokens']['total']}")


def _print_consistency(consistency: dict):
    def fmt_rate(v):
        return f"{v:.2f}" if v is not None else "-"

    print(f"\n=== Consistency ({consistency['runs']} runs/question) ===")
    print(f"eligible_questions={consistency['eligible_questions']} "
          f"flake_rate={fmt_rate(consistency['flake_rate'])} "
          f"consistent_rate={fmt_rate(consistency['consistent_rate'])} "
          f"distinct_sql_mean={consistency['distinct_sql_mean']}")
    flaky_keys = {(f["tenant_id"], f["id"]) for f in consistency["flaky_questions"]}
    for q in consistency["per_question"]:
        if (q["tenant_id"], q["id"]) not in flaky_keys:
            continue
        vector = ",".join("T" if o else "F" for o in q["row_match_outcomes"])
        print(f"  [FLAKY] {q['tenant_id']}:{q['id']} row_match=[{vector}] "
              f"distinct_sql={q['distinct_generated_sql']}")
    for q in consistency["per_question"]:
        if q["distinct_generated_sql"] > 1 and (q["tenant_id"], q["id"]) not in flaky_keys:
            print(f"  [SQL-VARIES] {q['tenant_id']}:{q['id']} "
                  f"distinct_sql={q['distinct_generated_sql']} "
                  f"(row_match outcome consistent)")


def _parse_args():
    parser = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter
    )
    parser.add_argument("--ids", help="Comma-separated selectors to run: 'tenant:id' "
                        "(qualified) or bare 'id' (allowed only when unique after filtering)")
    parser.add_argument("--limit", type=int, help="Run only the first N entries after "
                        "filtering (GLOBAL slice — in a combined run this may exclude whole "
                        "tenants; prefer --limit-per-tenant)")
    parser.add_argument("--limit-per-tenant", type=int, dest="limit_per_tenant",
                        help="Run only the first N entries of EACH tenant — keeps every "
                             "tenant represented in a combined run")
    parser.add_argument("--tenant", help="Run only entries for this tenant_id")
    parser.add_argument("--output", help="Artifact path (default: eval/results/eval_<timestamp>.json)")
    parser.add_argument("--runs", type=int, default=1,
                        help="Attempts per question, run consecutively; N>1 measures "
                             "run-to-run consistency. Multiplies Azure token cost and "
                             "wall time by N.")
    parser.add_argument("--dataset", help=f"Single dataset JSONL path (default: {DATASET_PATH}). "
                        "Mutually exclusive with --prod-tenants.")
    parser.add_argument("--prod-tenants", action="store_true", dest="prod_tenants",
                        help="Combined multi-tenant run over the files listed in the prod-tenant "
                             f"manifest ({MANIFEST_PATH.name}). Mutually exclusive with --dataset.")
    parser.add_argument("--manifest", help=f"Prod-tenant manifest path (default: {MANIFEST_PATH})")
    args = parser.parse_args()
    if args.runs < 1:
        parser.error("--runs must be >= 1")
    if args.dataset and args.prod_tenants:
        parser.error("--dataset and --prod-tenants are mutually exclusive")
    if args.limit and args.limit_per_tenant:
        parser.error("--limit and --limit-per-tenant are mutually exclusive")
    return args


def _fatal(header: str, problems: List[str]):
    print(f"ERROR: {header}", file=sys.stderr)
    for p in problems:
        print(f"  - {p}", file=sys.stderr)
    sys.exit(1)


def discover_dataset_paths(args) -> List[Path]:
    """Resolve which dataset files a run covers. Default: the single Default
    master. --dataset: one explicit file. --prod-tenants: the manifest's files.
    Paths are resolved before _load_deps() chdirs into src/."""
    if args.prod_tenants:
        manifest = Path(args.manifest).resolve() if args.manifest else MANIFEST_PATH
        if not manifest.exists():
            _fatal(f"prod-tenant manifest not found at {manifest}",
                   ["the prod dataset files are local-only; create the manifest "
                    "listing them to run --prod-tenants"])
        try:
            data = json.loads(manifest.read_text(encoding="utf-8"))
            paths = resolve_manifest_paths(data, manifest.parent)
        except Exception as e:
            _fatal(f"bad manifest {manifest}", [repr(e)])
        absent = [str(p) for p in paths if not p.exists()]
        if absent:
            _fatal("manifest references missing files", absent)
        return paths
    single = Path(args.dataset).resolve() if args.dataset else DATASET_PATH
    if not single.exists():
        _fatal(f"dataset not found at {single}", [])
    return [single]


def load_and_validate(paths: List[Path], light: SimpleNamespace,
                      prod_mode: bool) -> Tuple[List[dict], List[dict]]:
    """Load every file, validate structure, return (combined_entries, descriptors).
    Prod files must each be one non-Default tenant with a matching filename;
    (tenant_id, id) must be globally unique. Exits on any problem."""
    problems, descriptors, combined = [], [], []
    for path in paths:
        entries = light.load_entries(path)
        if prod_mode:
            problems += validate_prod_file(path, entries, light.default_tenant)
        descriptors.append(dataset_descriptor(path, entries))
        combined += entries
    problems += check_global_id_uniqueness(combined)
    if problems:
        _fatal("dataset validation failed", problems)
    return combined, descriptors


def select_entries(entries: List[dict], args, known_tenants: set) -> List[dict]:
    """Apply --tenant, --ids selectors, the capture filter, and limits, then
    strict-check that every selected tenant is configured (an unknown tenant
    would silently route to Default at config.py:191). Exits on error."""
    pool = entries
    if args.tenant:
        if args.tenant not in known_tenants:
            _fatal(f"--tenant '{args.tenant}' is not a configured tenant",
                   [f"known: {sorted(known_tenants)}"])
        pool = [e for e in pool if e["tenant_id"] == args.tenant]
    if args.ids:
        pool, errors = apply_selectors(pool, parse_selectors(args.ids))
        if errors:
            _fatal("--ids selection failed", errors)

    captured = []
    for e in pool:
        if not e.get("content_hash"):
            print(f"  [SKIP] {e['tenant_id']}:{e['id']}: not yet captured "
                  f"(run capture_dataset.py first)")
            continue
        captured.append(e)

    if args.limit_per_tenant:
        captured = limit_per_tenant(captured, args.limit_per_tenant)
    elif args.limit:
        captured = captured[: args.limit]
    if not captured:
        print("No entries selected.", file=sys.stderr)
        sys.exit(1)

    unknown = sorted({e["tenant_id"] for e in captured} - known_tenants)
    if unknown:
        _fatal(f"selected entries reference unconfigured tenants {unknown}",
               [f"known: {sorted(known_tenants)}"])
    return captured


def _print_done(record: dict):
    print(f"[Eval] done {record['tenant_id']}:{record['id']} measured={record.get('measured')} "
          f"valid={record.get('valid_execution')} row_match={record.get('row_match')} "
          f"canonical={record.get('row_match_canonical')} "
          f"hash_match={record.get('content_hash_match')} "
          f"tables={(record.get('table_scoring') or {}).get('match')} "
          f"iters={record.get('self_correction_iterations')} "
          f"latency_ms={record.get('latency_ms')} "
          f"tokens={(record.get('tokens') or {}).get('total_tokens')}"
          + (f" guard_violation={record['guard_violation']!r}" if record.get("guard_violation") else "")
          + (f" error={record.get('generation_error') or record.get('execution_error') or record.get('runner_error')!r}"
             if not record.get("row_match") else ""))


def _run_all(selected: List[dict], runs: int, deps: SimpleNamespace) -> List[dict]:
    records = []
    for i, entry in enumerate(selected, 1):
        for attempt in range(1, runs + 1):
            print(f"[Eval] start {i}/{len(selected)}"
                  + (f" attempt {attempt}/{runs}" if runs > 1 else "")
                  + f" {entry['tenant_id']}:{entry['id']} "
                  f"difficulty={entry['difficulty']} schema_type={entry['schema_type']}")
            try:
                record = run_one(entry, deps)
            except Exception as e:
                # One bad question never aborts the run.
                record = {
                    "id": entry["id"], "question": entry["question"],
                    "tenant_id": entry["tenant_id"], "schema_type": entry["schema_type"],
                    "difficulty": entry["difficulty"], "tags": entry.get("tags", []),
                    "frozen": entry.get("frozen", False),
                    "runner_error": repr(e), "measured": False,
                }
            record["attempt"] = attempt
            records.append(record)
            _print_done(record)
    return records


def main():
    args = _parse_args()
    output_override = Path(args.output).resolve() if args.output else None
    paths = discover_dataset_paths(args)

    # Light imports (config + load_entries, no pgvector) so all dataset
    # validation and selection fail fast before the heavy stack is loaded.
    light = _light_imports()
    combined, descriptors = load_and_validate(paths, light, bool(args.prod_tenants))
    known_tenants = set(light.config.tenant_mappings.keys())
    selected = select_entries(combined, args, known_tenants)

    if extract_tables("SELECT 1")[1] == "sqlglot_not_installed":
        print("WARNING: sqlglot not installed — table-selection scoring disabled "
              "(pip install -r eval/requirements.txt)")

    print("Loading live stack (Azure OpenAI + Metabase + Postgres/pgvector)...")
    deps = _load_deps()

    # Preflight: pgvector must have schema rows for every selected db_id, or
    # generate_sql silently returns Nones with no distinguishable cause.
    selected_tenants = {e["tenant_id"] for e in selected}
    db_ids = [deps.config.get_tenant_config(t)["db_id"] for t in selected_tenants]
    missing = preflight_embeddings(deps.sql_generator, db_ids)
    if missing:
        _fatal("embeddings preflight failed",
               [f"db_id {db_id}: {reason}" for db_id, reason in missing])

    started_at = datetime.now(timezone.utc)
    run_id = started_at.strftime("%Y%m%dT%H%M%SZ")
    print(f"Running eval on {len(selected)} entr{'y' if len(selected) == 1 else 'ies'}"
          + (f" x {args.runs} attempts" if args.runs > 1 else "")
          + f" across {len(selected_tenants)} tenant(s) {sorted(selected_tenants)}"
          + f" (run_id={run_id})...\n")

    records = _run_all(selected, args.runs, deps)

    finished_at = datetime.now(timezone.utc)
    aggregates = build_aggregates(records)
    consistency = build_consistency(records, args.runs)
    artifact = {
        "eval_schema_version": EVAL_SCHEMA_VERSION,
        "run_id": run_id,
        "started_at": started_at.isoformat(timespec="seconds"),
        "finished_at": finished_at.isoformat(timespec="seconds"),
        "git_commit": _git_commit(),
        "sqlglot_version": _sqlglot_version(),
        "datasets": descriptors,
        "dataset_set_sha256": dataset_set_sha256(descriptors),
        "tenants": sorted(selected_tenants),
        "entries_selected": len(selected),
        "runs": args.runs,
        "filters": {"ids": args.ids, "limit": args.limit,
                    "limit_per_tenant": args.limit_per_tenant,
                    "tenant": args.tenant, "prod_tenants": bool(args.prod_tenants)},
        "config_snapshot": {
            "deployment": deps.config.ai.azure_deployment,
            "k_samples": deps.config.ai.k_samples,
            "retry_k_samples": deps.config.ai.retry_k_samples,
            "max_self_correction_iterations": deps.config.ai.max_self_correction_iterations,
        },
        "questions": records,
        "aggregates": aggregates,
        # Always present; null for --runs 1 so the artifact shape is stable.
        "consistency": consistency,
    }

    output_path = output_override if output_override else RESULTS_DIR / f"eval_{run_id}.json"
    output_path.parent.mkdir(parents=True, exist_ok=True)
    with open(output_path, "w", encoding="utf-8") as f:
        json.dump(artifact, f, indent=2, ensure_ascii=False)
        f.write("\n")

    _print_summary(aggregates)
    if consistency is not None:
        _print_consistency(consistency)
    print(f"\nWrote {output_path}")

    unmeasured = aggregates["overall"]["unmeasured"]
    safety_violations = aggregates["overall"]["safety_violations"]
    if unmeasured:
        print(f"EXIT 1: {unmeasured} question(s) could not be measured "
              f"(gold SQL failed live or runner error).", file=sys.stderr)
        sys.exit(1)
    if safety_violations:
        print(f"EXIT 2: {safety_violations} guard/safety violation(s) — see "
              f"guard_violation on the affected records.", file=sys.stderr)
        sys.exit(2)


if __name__ == "__main__":
    main()
