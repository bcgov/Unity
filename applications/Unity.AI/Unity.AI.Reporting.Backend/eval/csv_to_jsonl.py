"""
Sync SME-authored rows in questions_template.csv into questions.jsonl.
Pure offline transform — no network calls, no Metabase/DB access.

Two-way by `id`:
  - Row's `id` column blank -> treated as a brand new question. An id is
    auto-generated per stratification cell (<TIER>-<DIFFICULTY>-<seq>),
    continuing from the highest existing sequence number so repeated runs
    don't collide. Expected-result fields are left empty for capture_dataset.py.
  - Row's `id` column filled in -> updates that existing entry's editable
    fields in place. If `gold_sql` changed from what's stored, the entry's
    expected-result fields are reset to the empty stub (and captured_at
    cleared) so capture_dataset.py knows to re-run it — an edited query with
    a stale frozen answer would silently pass verify_dataset.py otherwise.
    An `id` that doesn't match any existing entry is an error (likely a typo
    or a row for an entry that was deleted from questions.jsonl).

CSV columns (header row required):
    id, question, tenant_id, schema_type, difficulty, gold_sql,
    tags, notes, schema_version, frozen, question_type

- id may be blank (new question) or an existing id (update)
- gold_sql may be written across multiple lines inside its cell (jsonl_to_csv.py
  exports it that way for review); it is collapsed back to a single line before
  storing, so line breaks alone never count as an edit. SQL comments (-- or /*)
  aren't allowed, since collapsing would comment out the rest of the query.
- tags is a semicolon-separated list (e.g. "join;date")
- frozen is "true"/"false" (default "false" if blank)
- question_type is "standard"/"negative"/"ambiguous" (default "standard" if blank)
- any extra columns (e.g. row_count, captured_at from jsonl_to_csv.py) are ignored

Deleting a question isn't supported via CSV — remove its line from
questions.jsonl directly.

Usage:
    python eval/csv_to_jsonl.py eval/dataset/questions_template.csv eval/dataset/questions.jsonl
"""
import argparse
import csv
import json
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent.parent / "src"))

from config import config  # noqa: E402
from sql_format import collapse_sql, has_comment  # noqa: E402

TIER_BY_SCHEMA_TYPE = {
    "public": "PUB",
    "worksheet": "WKS",
    "scoresheet": "WKS",
}

EMPTY_EXPECTED_RESULT = {
    "expected_columns": [],
    "expected_column_types": [],
    "expected_row_count": None,
    "content_hash": None,
}


def load_existing_entries(path: Path) -> list:
    if not path.exists():
        return []
    with open(path, "r", encoding="utf-8") as f:
        return json.load(f)


def next_seq_by_prefix(entries: list) -> dict:
    """Returns {'PUB-EASY': 10, 'WKS-HARD': 11, ...} highest seq seen per <TIER>-<DIFFICULTY> prefix."""
    seqs = {}
    for entry in entries:
        parts = entry["id"].rsplit("-", 1)
        if len(parts) != 2:
            continue
        prefix, seq_str = parts
        try:
            seq = int(seq_str)
        except ValueError:
            continue
        seqs[prefix] = max(seqs.get(prefix, 0), seq)
    return seqs


def parse_list_field(value: str) -> list:
    return [v.strip() for v in value.split(";") if v.strip()]


def gold_sql_from_row(row: dict) -> str:
    """Collapses the (possibly pretty-printed) cell back to the canonical
    one-line form stored in questions.jsonl."""
    gold_sql = row["gold_sql"]
    if has_comment(gold_sql):
        raise ValueError(
            f"gold_sql for '{(row.get('id') or row['question']).strip()}' contains an SQL "
            "comment (-- or /*). gold_sql is stored on a single line, which would comment "
            "out the rest of the query — put the explanation in the notes column instead."
        )
    return collapse_sql(gold_sql)


def shared_fields_from_row(row: dict) -> dict:
    schema_type = row["schema_type"].strip().lower()
    tenant_id = row["tenant_id"].strip()
    config.get_tenant_config(tenant_id)  # validates the tenant exists
    return {
        "question": row["question"].strip(),
        "tenant_id": tenant_id,
        "schema_type": schema_type,
        "difficulty": row["difficulty"].strip().lower(),
        "question_type": (row.get("question_type") or "standard").strip() or "standard",
        "gold_sql": gold_sql_from_row(row),
        "frozen": (row.get("frozen") or "false").strip().lower() == "true",
        "tags": parse_list_field(row["tags"]),
        "schema_version": row["schema_version"].strip(),
        "notes": row.get("notes", "").strip(),
    }


def new_entry_from_row(row: dict, seq_counters: dict) -> dict:
    fields = shared_fields_from_row(row)
    tier = TIER_BY_SCHEMA_TYPE.get(fields["schema_type"])
    if tier is None:
        raise ValueError(f"Unknown schema_type '{fields['schema_type']}' (expected public/worksheet/scoresheet)")

    prefix = f"{tier}-{fields['difficulty'].upper()}"
    seq_counters[prefix] = seq_counters.get(prefix, 0) + 1
    entry_id = f"{prefix}-{seq_counters[prefix]:03d}"

    return {
        "id": entry_id,
        "question": fields["question"],
        "tenant_id": fields["tenant_id"],
        "difficulty": fields["difficulty"],
        "gold_sql": fields["gold_sql"],
        **EMPTY_EXPECTED_RESULT,
        "frozen": fields["frozen"],
        "schema_type": fields["schema_type"],
        "tags": fields["tags"],
        "question_type": fields["question_type"],
        "schema_version": fields["schema_version"],
        "notes": fields["notes"],
        "captured_at": None,
    }


def apply_update_to_entry(entry: dict, row: dict) -> bool:
    """Updates entry's editable fields in place. Returns True if gold_sql
    changed (caller should note it needs re-capturing)."""
    fields = shared_fields_from_row(row)
    gold_sql_changed = fields["gold_sql"] != entry["gold_sql"]
    entry.update(fields)
    if gold_sql_changed:
        entry.update(EMPTY_EXPECTED_RESULT)
        entry["captured_at"] = None
    return gold_sql_changed


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("csv_path", help="Input questions_template.csv")
    parser.add_argument("jsonl_path", help="Output questions.jsonl (updated in place + appended)")
    args = parser.parse_args()

    csv_path = Path(args.csv_path)
    jsonl_path = Path(args.jsonl_path)

    existing_entries = load_existing_entries(jsonl_path)
    existing_by_id = {e["id"]: e for e in existing_entries}
    seq_counters = next_seq_by_prefix(existing_entries)

    appended_entries = []
    updated_ids = []
    needs_recapture = []

    with open(csv_path, "r", encoding="utf-8", newline="") as f:
        reader = csv.DictReader(f)
        for row in reader:
            row_id = (row.get("id") or "").strip()
            if not row_id:
                new_entry = new_entry_from_row(row, seq_counters)
                appended_entries.append(new_entry)
                continue
            if row_id not in existing_by_id:
                raise ValueError(
                    f"CSV row has id '{row_id}' which doesn't exist in {jsonl_path}. "
                    "Leave id blank to author a new question, or fix the typo if you "
                    "meant to update an existing one."
                )
            gold_sql_changed = apply_update_to_entry(existing_by_id[row_id], row)
            updated_ids.append(row_id)
            if gold_sql_changed:
                needs_recapture.append(row_id)

    all_entries = existing_entries + appended_entries
    jsonl_path.parent.mkdir(parents=True, exist_ok=True)
    with open(jsonl_path, "w", encoding="utf-8") as f:
        json.dump(all_entries, f, indent=2, ensure_ascii=False)
        f.write("\n")

    print(f"Updated {len(updated_ids)} existing entries, added {len(appended_entries)} new entries -> {jsonl_path}")
    for entry in appended_entries:
        print(f"  [new] {entry['id']}: {entry['question']}")
    if needs_recapture:
        print(f"\n{len(needs_recapture)} updated entr{'y' if len(needs_recapture) == 1 else 'ies'} had gold_sql "
              f"change — expected-result fields were reset. Re-run capture_dataset.py to refresh them:")
        for entry_id in needs_recapture:
            print(f"  - {entry_id}")


if __name__ == "__main__":
    main()
