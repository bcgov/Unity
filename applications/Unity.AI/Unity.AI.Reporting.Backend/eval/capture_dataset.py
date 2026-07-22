"""
Fill in `expected_result` for questions.jsonl entries by executing their
`gold_sql` via Metabase and recording a content signature (columns,
column_types, row_count, content_hash) — never raw rows, to keep applicant
data (worksheet/scoresheet PII) out of a version-controlled file.

Only processes entries missing `expected_result.content_hash`, unless
--refresh/--refresh-all is passed. Requires live Metabase network access and
a tenant_config.local.json api_key for the entry's tenant — run by whoever has
that access (see eval/README.md).

Usage:
    python eval/capture_dataset.py                  # fill in missing entries only
    python eval/capture_dataset.py --refresh WKS-HARD-004
    python eval/capture_dataset.py --refresh-all
"""
import argparse
import hashlib
import json
import sys
from datetime import datetime, timezone
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent.parent / "src"))

from config import config  # noqa: E402
from metabase import metabase_client  # noqa: E402

DATASET_PATH = Path(__file__).parent / "dataset" / "questions.jsonl"


def compute_content_hash(cols: list, rows: list) -> str:
    """Deterministic hash over column names + row values (not stored raw).
    Values are stringified so Decimal/datetime/UUID objects hash consistently
    regardless of the Python type Metabase's JSON decodes them into."""
    payload = {
        "columns": [c["name"] for c in cols],
        "rows": [[str(v) for v in row] for row in rows],
    }
    canonical = json.dumps(payload, sort_keys=True, separators=(",", ":"))
    return "sha256:" + hashlib.sha256(canonical.encode("utf-8")).hexdigest()


def load_entries(dataset_path: Path = DATASET_PATH) -> list:
    with open(dataset_path, "r", encoding="utf-8") as f:
        return json.load(f)


def write_entries(entries: list, dataset_path: Path = DATASET_PATH):
    with open(dataset_path, "w", encoding="utf-8") as f:
        json.dump(entries, f, indent=2, ensure_ascii=False)
        f.write("\n")


def capture_one(entry: dict) -> tuple:
    """Returns (ok, message)."""
    tenant_id = entry["tenant_id"]
    db_id = config.get_tenant_config(tenant_id)["db_id"]
    try:
        result = metabase_client.execute_sql(entry["gold_sql"], db_id, tenant_id=tenant_id)
    except Exception as e:
        return False, f"execution failed: {e}"

    cols = result.get("cols", [])
    rows = result.get("rows", [])
    entry["expected_columns"] = [c["name"] for c in cols]
    entry["expected_column_types"] = [c.get("base_type", "unknown") for c in cols]
    entry["expected_row_count"] = len(rows)
    entry["content_hash"] = compute_content_hash(cols, rows)
    entry["captured_at"] = datetime.now(timezone.utc).isoformat(timespec="seconds")
    return True, f"row_count={len(rows)}"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--refresh", help="Re-capture a single entry by id, even if already captured")
    parser.add_argument("--refresh-all", action="store_true", help="Re-capture every entry, even if already captured")
    parser.add_argument("--dataset", help=f"Dataset JSONL path (default: {DATASET_PATH})")
    args = parser.parse_args()
    dataset_path = Path(args.dataset).resolve() if args.dataset else DATASET_PATH

    if not dataset_path.exists():
        print(f"ERROR: dataset not found at {dataset_path}", file=sys.stderr)
        sys.exit(1)

    entries = load_entries(dataset_path)
    to_process = []
    for entry in entries:
        already_captured = bool(entry.get("content_hash"))
        if args.refresh_all:
            to_process.append(entry)
        elif args.refresh and entry["id"] == args.refresh:
            to_process.append(entry)
        elif not args.refresh and not args.refresh_all and not already_captured:
            to_process.append(entry)

    if not to_process:
        print("Nothing to capture (use --refresh <id> or --refresh-all to force).")
        return

    print(f"Capturing {len(to_process)} entr{'y' if len(to_process) == 1 else 'ies'}...")
    failures = []
    for entry in to_process:
        ok, message = capture_one(entry)
        status = "OK" if ok else "FAILED"
        print(f"  [{status}] {entry['id']}: {message}")
        if not ok:
            failures.append((entry["id"], message))

    write_entries(entries, dataset_path)
    print(f"Wrote {dataset_path}")

    if failures:
        print(f"\n{len(failures)} entr{'y' if len(failures) == 1 else 'ies'} failed to capture:", file=sys.stderr)
        for entry_id, message in failures:
            print(f"  - {entry_id}: {message}", file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()
