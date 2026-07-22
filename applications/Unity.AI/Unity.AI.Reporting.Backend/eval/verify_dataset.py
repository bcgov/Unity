"""
Re-run every `gold_sql` in questions.jsonl against live Metabase and check it
still matches the stored expected_result signature.

- Always checked: the query still executes, and returned columns/column_types
  match what was captured.
- `frozen: true` entries additionally require row_count and content_hash to
  match exactly — any mismatch is a hard failure (these are anchored to
  closed/historical data that shouldn't drift).
- `frozen: false` entries log row_count/content_hash drift without failing
  (they're expected to grow on live data).

Exits non-zero if any hard failure occurred. Usable today as a manual check;
the natural hook for a future CI job (not wired up by this ticket).

Usage:
    python eval/verify_dataset.py
"""
import argparse
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent.parent / "src"))

from config import config  # noqa: E402
from metabase import metabase_client  # noqa: E402
from capture_dataset import DATASET_PATH, compute_content_hash, load_entries  # noqa: E402


def verify_one(entry: dict) -> list:
    """Returns a list of (is_hard_failure, message) problems found, empty if clean."""
    if not entry.get("content_hash"):
        return [(False, "not yet captured — skip (run capture_dataset.py first)")]

    db_id = config.get_tenant_config(entry["tenant_id"])["db_id"]
    try:
        result = metabase_client.execute_sql(entry["gold_sql"], db_id, tenant_id=entry["tenant_id"])
    except Exception as e:
        return [(True, f"query failed to execute: {e}")]

    problems = []
    cols = result.get("cols", [])
    rows = result.get("rows", [])
    actual_columns = [c["name"] for c in cols]
    actual_types = [c.get("base_type", "unknown") for c in cols]

    if actual_columns != entry.get("expected_columns"):
        problems.append((True, f"columns changed: expected {entry.get('expected_columns')}, got {actual_columns}"))
    if actual_types != entry.get("expected_column_types"):
        problems.append((True, f"column_types changed: expected {entry.get('expected_column_types')}, got {actual_types}"))

    actual_row_count = len(rows)
    actual_hash = compute_content_hash(cols, rows)
    row_count_mismatch = actual_row_count != entry.get("expected_row_count")
    hash_mismatch = actual_hash != entry.get("content_hash")

    if row_count_mismatch or hash_mismatch:
        is_hard = bool(entry.get("frozen"))
        detail = []
        if row_count_mismatch:
            detail.append(f"row_count expected={entry.get('expected_row_count')} actual={actual_row_count}")
        if hash_mismatch:
            detail.append("content_hash differs")
        problems.append((is_hard, "; ".join(detail)))

    return problems


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dataset", help=f"Dataset JSONL path (default: {DATASET_PATH})")
    args = parser.parse_args()
    dataset_path = Path(args.dataset).resolve() if args.dataset else DATASET_PATH
    entries = load_entries(dataset_path)
    print(f"Verifying {len(entries)} entries against live Metabase...\n")

    hard_failures = 0
    soft_drift = 0
    for entry in entries:
        problems = verify_one(entry)
        if not problems:
            print(f"  [OK] {entry['id']}")
            continue
        for is_hard, message in problems:
            if is_hard:
                hard_failures += 1
                print(f"  [FAIL] {entry['id']}: {message}")
            else:
                soft_drift += 1
                print(f"  [DRIFT] {entry['id']}: {message}")

    print(f"\n{len(entries)} entries checked — {hard_failures} hard failures, {soft_drift} drift/skip notices.")
    if hard_failures:
        sys.exit(1)


if __name__ == "__main__":
    main()
