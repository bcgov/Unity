"""
Evaluation runner for the NL->SQL pipeline: exercises sql_generator.generate_sql
against eval/dataset/questions.jsonl and scores the generated SQL's live results
against a live re-run of each entry's gold_sql.

Metrics (per question and aggregated overall / by difficulty / by schema_type /
by difficulty x schema_type / by tag):
- execution validity (generated SQL runs without error)
- execution accuracy: row-content match (row order + column order/aliases
  ignored) as the primary rule, strict content-hash match as a stricter
  secondary number
- self-correction iterations (first-attempt success rate, distribution)
- latency (wall-clock per generate_sql call; mean/p50/p95)
- token usage

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

Usage:
    python eval/run_eval.py                        # all captured entries
    python eval/run_eval.py --ids PUB-EASY-001,WKS-HARD-004
    python eval/run_eval.py --limit 5
    python eval/run_eval.py --tenant "Default Grants Program"
    python eval/run_eval.py --output path.json
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
from pathlib import Path
from types import SimpleNamespace
from typing import Dict, List, Optional, Tuple

DATASET_PATH = Path(__file__).parent / "dataset" / "questions.jsonl"
RESULTS_DIR = Path(__file__).parent / "results"

# Version of THIS artifact format — bump when the JSON schema below changes,
# so tooling comparing runs across time can detect incompatible artifacts.
EVAL_SCHEMA_VERSION = "1"

PERMUTATION_CAP = 5000

# ---------------------------------------------------------------------------
# Pure, import-safe functions (offline unit-tested by test_run_eval.py).
# Heavy imports (config / metabase / sql_generator, which need live Postgres)
# are deferred into _load_deps() so importing this module needs no infra.
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
    r"|MERGE|COPY|CALL|DO|VACUUM|REINDEX|LOCK|SET|RESET|COMMENT)\b",
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


def stringify_rows(rows: List[list]) -> List[Tuple[str, ...]]:
    """str() every value — same convention as capture_dataset.compute_content_hash,
    so NULL->'None', Decimal/datetime formatting etc. are symmetric on both sides.
    Deliberately strict: '1' != '1.0' and '2026-07-08' != '2026-07-08T00:00:00'."""
    return [tuple(str(v) for v in row) for row in rows]


class _PermutationCapExceeded(Exception):
    pass


def rows_match(gold_rows: List[list], gen_rows: List[list],
               gold_ncols: int, gen_ncols: int,
               permutation_cap: int = PERMUTATION_CAP) -> Tuple[bool, str]:
    """Primary accuracy rule: permutation row-multiset match.

    Match = same column count AND some column permutation of the generated
    rows yields the same multiset of stringified row-tuples as gold. Row
    order and column names/aliases are ignored; row-wise value pairing is
    preserved exactly (unlike sorting values within each row, which would
    false-positive on cross-column transpositions).

    Returns (matched, reason) — reason is "exact" / "column_permutation" on
    match, or "column_count_mismatch" / "row_count_mismatch" /
    "row_content_mismatch" / "permutation_search_exhausted" on miss.
    """
    if gold_ncols != gen_ncols:
        return False, "column_count_mismatch"
    gold_s = stringify_rows(gold_rows)
    gen_s = stringify_rows(gen_rows)
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

    return {
        "n": n,
        "unmeasured": n - m,
        "safety_violations": sum(1 for r in records if r.get("guard_violation")),
        "validity_rate": _rate(sum(1 for r in measured if r.get("valid_execution")), m),
        "row_match_rate": _rate(len(successes), m),
        "hash_match_rate": _rate(sum(1 for r in measured if r.get("content_hash_match")), m),
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
        "by_difficulty": strata(lambda r: [r["difficulty"]]),
        "by_schema_type": strata(lambda r: [r["schema_type"]]),
        "by_difficulty_schema": strata(lambda r: [f"{r['difficulty']}/{r['schema_type']}"]),
        "by_tag": strata(lambda r: r.get("tags") or []),
    }


# ---------------------------------------------------------------------------
# Runtime — everything below needs the live stack.
# ---------------------------------------------------------------------------

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


def _dataset_fingerprint(entries: List[dict]) -> dict:
    return {
        "sha256": "sha256:" + hashlib.sha256(DATASET_PATH.read_bytes()).hexdigest(),
        "schema_versions": sorted({e.get("schema_version", "") for e in entries}),
    }


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
        "content_hash_match": False,
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
        print(f"  [DRIFT-FROZEN] {entry['id']}: frozen entry's live gold result "
              f"differs from stored content_hash — run verify_dataset.py")
    record["measured"] = True

    # 5. Compare.
    if gen_cols is not None:
        matched, reason = rows_match(gold_rows, gen_rows, len(gold_cols), len(gen_cols))
        record["row_match"] = matched
        record["row_match_reason"] = reason
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
    rows += [(k, v) for k, v in aggregates["by_difficulty"].items()]
    rows += [(k, v) for k, v in aggregates["by_schema_type"].items()]

    print("\n=== Eval summary ===")
    header = (f"{'stratum':<22}{'n':>4}{'valid':>7}{'row_match':>11}"
              f"{'hash_match':>12}{'iters':>7}{'p50ms':>9}{'p95ms':>9}{'tok/q':>8}")
    print(header)
    print("-" * len(header))
    for name, a in rows:
        print(f"{name:<22}{a['n']:>4}{fmt_rate(a['validity_rate']):>7}"
              f"{fmt_rate(a['row_match_rate']):>11}{fmt_rate(a['hash_match_rate']):>12}"
              f"{fmt(a['iterations']['mean']):>7}{fmt(a['latency_ms']['p50']):>9}"
              f"{fmt(a['latency_ms']['p95']):>9}{fmt(a['tokens']['mean_per_question']):>8}")
    overall = aggregates["overall"]
    print(f"\nunmeasured={overall['unmeasured']} safety_violations={overall['safety_violations']} "
          f"first_attempt_success_rate={fmt_rate(overall['first_attempt_success_rate'])} "
          f"total_tokens={overall['tokens']['total']}")


def main():
    parser = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter
    )
    parser.add_argument("--ids", help="Comma-separated entry ids to run (default: all captured)")
    parser.add_argument("--limit", type=int, help="Run only the first N entries after filtering")
    parser.add_argument("--tenant", help="Run only entries for this tenant_id")
    parser.add_argument("--output", help="Artifact path (default: eval/results/eval_<timestamp>.json)")
    args = parser.parse_args()

    if not DATASET_PATH.exists():
        print(f"ERROR: dataset not found at {DATASET_PATH}", file=sys.stderr)
        sys.exit(1)

    # Resolve before _load_deps() chdirs into src/ — a relative --output
    # should be relative to where the user launched the script.
    output_override = Path(args.output).resolve() if args.output else None

    print("Loading live stack (Azure OpenAI + Metabase + Postgres/pgvector)...")
    deps = _load_deps()
    entries = deps.load_entries()

    selected = []
    wanted_ids = {i.strip() for i in args.ids.split(",")} if args.ids else None
    for entry in entries:
        if wanted_ids is not None and entry["id"] not in wanted_ids:
            continue
        if args.tenant and entry["tenant_id"] != args.tenant:
            continue
        if not entry.get("content_hash"):
            print(f"  [SKIP] {entry['id']}: not yet captured (run capture_dataset.py first)")
            continue
        selected.append(entry)
    if wanted_ids:
        missing = wanted_ids - {e["id"] for e in selected}
        if missing:
            print(f"ERROR: ids not found or not captured: {sorted(missing)}", file=sys.stderr)
            sys.exit(1)
    if args.limit:
        selected = selected[: args.limit]
    if not selected:
        print("No entries selected.", file=sys.stderr)
        sys.exit(1)

    started_at = datetime.now(timezone.utc)
    run_id = started_at.strftime("%Y%m%dT%H%M%SZ")
    print(f"Running eval on {len(selected)} entr{'y' if len(selected) == 1 else 'ies'} (run_id={run_id})...\n")

    records = []
    for i, entry in enumerate(selected, 1):
        print(f"[Eval] start {i}/{len(selected)} id={entry['id']} "
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
        records.append(record)
        print(f"[Eval] done id={record['id']} measured={record.get('measured')} "
              f"valid={record.get('valid_execution')} row_match={record.get('row_match')} "
              f"hash_match={record.get('content_hash_match')} "
              f"iters={record.get('self_correction_iterations')} "
              f"latency_ms={record.get('latency_ms')} "
              f"tokens={(record.get('tokens') or {}).get('total_tokens')}"
              + (f" guard_violation={record['guard_violation']!r}" if record.get("guard_violation") else "")
              + (f" error={record.get('generation_error') or record.get('execution_error') or record.get('runner_error')!r}"
                 if not record.get("row_match") else ""))

    finished_at = datetime.now(timezone.utc)
    aggregates = build_aggregates(records)
    artifact = {
        "eval_schema_version": EVAL_SCHEMA_VERSION,
        "run_id": run_id,
        "started_at": started_at.isoformat(timespec="seconds"),
        "finished_at": finished_at.isoformat(timespec="seconds"),
        "git_commit": _git_commit(),
        "dataset_path": str(DATASET_PATH),
        "dataset_fingerprint": _dataset_fingerprint(entries),
        "tenants": sorted({e["tenant_id"] for e in selected}),
        "entries_selected": len(selected),
        "filters": {"ids": args.ids, "limit": args.limit, "tenant": args.tenant},
        "config_snapshot": {
            "deployment": deps.config.ai.azure_deployment,
            "k_samples": deps.config.ai.k_samples,
            "retry_k_samples": deps.config.ai.retry_k_samples,
            "max_self_correction_iterations": deps.config.ai.max_self_correction_iterations,
        },
        "questions": records,
        "aggregates": aggregates,
    }

    output_path = output_override if output_override else RESULTS_DIR / f"eval_{run_id}.json"
    output_path.parent.mkdir(parents=True, exist_ok=True)
    with open(output_path, "w", encoding="utf-8") as f:
        json.dump(artifact, f, indent=2, ensure_ascii=False)
        f.write("\n")

    _print_summary(aggregates)
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
