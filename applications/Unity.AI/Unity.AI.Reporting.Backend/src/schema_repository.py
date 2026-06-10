"""
Schema/metadata read layer for the data model generator.

All the read-only round-trips to Metabase (database metadata, ReportColumnsMaps
labels + correlation providers, per-column samples, FK probing, data-existence
checks) live here so the generator can focus on building SQL and orchestrating
the AI calls. Two short-TTL caches de-duplicate the metadata and ReportColumnsMaps
fetches that a single preview otherwise repeats.

Every method here is a pure read — callers treat returned dicts as read-only.
"""
import logging
import threading
import time
from typing import Optional

from metabase import metabase_client

logger = logging.getLogger(__name__)


class SchemaRepository:
    """Cached, read-only access to Metabase schema/label/sample data per tenant."""

    # Short TTL cache for Metabase database metadata. The same metadata is fetched
    # multiple times within a single preview (and once per view for combined models);
    # a brief TTL de-dupes those round-trips while staying fresh for schema edits.
    _META_CACHE_TTL = 60  # seconds

    _BATCH_DATA_CHECK_SIZE = 100

    def __init__(self) -> None:
        # key: (db_id, tenant_id) → (fetched_at_monotonic, metadata_dict)
        self._meta_cache: dict[tuple[int, str], tuple[float, dict]] = {}
        self._meta_lock = threading.Lock()
        # ReportColumnsMaps info (label set + correlation-provider map), same TTL —
        # de-dupes the per-view source-type lookups within a single preview.
        self._rcm_cache: dict[tuple[int, str], tuple[float, tuple[set, dict]]] = {}
        self._rcm_lock = threading.Lock()

    def get_metadata(self, db_id: int, tenant_id: str) -> dict:
        """Return Metabase DB metadata, cached per (db_id, tenant_id) for a short TTL.

        Callers treat the returned dict as read-only — it is shared across the cache
        window, so it must not be mutated.
        """
        key = (int(db_id), str(tenant_id).strip())
        now = time.monotonic()
        with self._meta_lock:
            cached = self._meta_cache.get(key)
            if cached and (now - cached[0]) < self._META_CACHE_TTL:
                return cached[1]
            metadata = metabase_client.get_database_metadata(db_id, tenant_id=tenant_id)
            self._meta_cache[key] = (now, metadata)
            return metadata

    def get_report_columns_maps_info(
        self, db_id: int, tenant_id: str
    ) -> tuple[set[str], dict[str, str]]:
        """Fetch (views_with_labels, correlation_providers) from ReportColumnsMaps.

        One round-trip, cached per (db_id, tenant_id) for the same short TTL as
        metadata. Source-type detection reads this once per view, so caching collapses
        those repeated lookups to a single query within a preview.

        - views_with_labels: set of ViewName values (for the has_labels flag)
        - correlation_providers: {ViewName: lowercase CorrelationProvider}
        """
        key = (int(db_id), str(tenant_id).strip())
        now = time.monotonic()
        with self._rcm_lock:
            cached = self._rcm_cache.get(key)
            if cached and (now - cached[0]) < self._META_CACHE_TTL:
                return cached[1]

            sql = 'SELECT DISTINCT "ViewName", "CorrelationProvider" FROM "Reporting"."ReportColumnsMaps"'
            views_with_labels: set[str] = set()
            correlation_providers: dict[str, str] = {}
            try:
                result = metabase_client.execute_sql(sql, db_id, tenant_id=tenant_id)
                for row in result.get("rows", []):
                    if row and row[0]:
                        views_with_labels.add(row[0])
                        if len(row) > 1 and row[1]:
                            correlation_providers[row[0]] = row[1].strip().lower()
            except Exception as e:
                logger.warning(
                    "discover_views: ReportColumnsMaps query failed (%s); "
                    "assuming no labels and no correlation providers",
                    e,
                )

            info = (views_with_labels, correlation_providers)
            self._rcm_cache[key] = (now, info)
            return info

    def get_view_labels(self, view_name: str, db_id: int, tenant_id: str) -> dict:
        """Fetch {col_name: {label, forms_type}} from ReportColumnsMaps."""
        # Metabase native query (not psycopg3) — escape the single-quoted literal
        # by doubling apostrophes, matching batch_check_view_data's literal escaper.
        view_name_literal = view_name.replace("'", "''")
        sql = f"""
        SELECT
            row_data->>'ColumnName' AS column_name,
            row_data->>'Label'      AS label,
            row_data->>'Type'       AS forms_type
        FROM "Reporting"."ReportColumnsMaps" rcm,
             jsonb_array_elements(rcm."Mapping"->'Rows') AS row_data
        WHERE rcm."ViewName" = '{view_name_literal}'
        """
        try:
            result = metabase_client.execute_sql(sql, db_id, tenant_id=tenant_id)
            return {
                row[0]: {"label": row[1], "forms_type": row[2]}
                for row in result.get("rows", [])
                if row[0]
            }
        except Exception as e:
            logger.warning(f"Could not fetch labels for view {view_name}: {e}")
            return {}

    def get_custom_field_labels(self, db_id: int, tenant_id: str) -> dict:
        """Fallback: fetch {key: label} from Flex.CustomFields."""
        sql = 'SELECT "Key", "Label" FROM "Flex"."CustomFields" WHERE "Key" IS NOT NULL'
        try:
            result = metabase_client.execute_sql(sql, db_id, tenant_id=tenant_id)
            return {row[0]: row[1] for row in result.get("rows", []) if row[0]}
        except Exception as e:
            logger.warning(f"Could not fetch custom field labels: {e}")
            return {}

    def find_application_id_column(self, schema: str, view_name: str,
                                   db_id: int, tenant_id: str) -> Optional[str]:
        """Check if the view has an application ID column by querying it directly."""
        for col_name in ("ApplicationId", "application_id"):
            sql = f'SELECT "{col_name}" FROM "{schema}"."{view_name}" LIMIT 1'
            try:
                result = metabase_client.execute_sql(sql, db_id, tenant_id=tenant_id)
                if "error" not in result:
                    logger.info(f"Found {col_name} column in {schema}.{view_name}")
                    return col_name
            except Exception as e:
                logger.debug(f"{col_name} not found in {schema}.{view_name}: {e}")
        return None

    def get_sample_value(self, schema: str, table: str, column: str,
                         db_id: int, tenant_id: str) -> Optional[str]:
        """Get a single sample value for a column."""
        sql = (
            f'SELECT "{column}" FROM "{schema}"."{table}" '
            f"WHERE \"{column}\" IS NOT NULL AND \"{column}\" <> '' LIMIT 1"
        )
        try:
            result = metabase_client.execute_sql(sql, db_id, tenant_id=tenant_id)
            rows = result.get("rows", [])
            if rows and rows[0][0]:
                return str(rows[0][0])
        except Exception:
            pass
        return None

    @staticmethod
    def _normalize_sample(raw) -> Optional[str]:
        """Clean a single cell value into a non-empty sample string, stripping
        a JSON-array wrapper when present. Returns None for empty/null cells."""
        if raw is None:
            return None
        val = str(raw).strip()
        if not val:
            return None
        if val.startswith('["') and val.endswith('"]'):
            val = val[2:-2]
        return val or None

    def get_column_samples(self, schema: str, table: str, columns: list[str],
                           db_id: int, tenant_id: str) -> dict[str, list[str]]:
        """Get 2-3 non-null sample values per column for type inference and AI context."""
        if not columns:
            return {}

        capped = columns[:50]
        col_refs = ", ".join(f'"{c}"' for c in capped)
        sql = f'SELECT {col_refs} FROM "{schema}"."{table}" LIMIT 3'

        samples: dict[str, list[str]] = {c: [] for c in capped}
        try:
            result = metabase_client.execute_sql(sql, db_id, tenant_id=tenant_id)
        except Exception as e:
            logger.warning(f"Could not fetch column samples from {schema}.{table}: {e}")
            return samples

        for row in result.get("rows", []):
            for i, col in enumerate(capped):
                if i >= len(row) or len(samples[col]) >= 3:
                    continue
                value = self._normalize_sample(row[i])
                if value:
                    samples[col].append(value)
        return samples

    def columns_needing_unwrap(self, schema: str, table: str, columns: list[str],
                               db_id: int, tenant_id: str) -> set[str]:
        """
        Return the set of column names whose stored values are JSON-array-wrapped
        (i.e. start with '["'). Detected per-column from raw (un-stripped) samples.
        """
        if not columns:
            return set()

        capped = columns[:50]
        col_refs = ", ".join(f'"{c}"' for c in capped)
        sql = f'SELECT {col_refs} FROM "{schema}"."{table}" LIMIT 5'
        try:
            result = metabase_client.execute_sql(sql, db_id, tenant_id=tenant_id)
        except Exception as e:
            logger.warning(f"Could not detect JSON unwrap for {schema}.{table}: {e}")
            return set()

        wrapped: set[str] = set()
        for row in result.get("rows", []):
            for i, col in enumerate(capped):
                if col in wrapped or i >= len(row) or row[i] is None:
                    continue
                if str(row[i]).strip().startswith('["'):
                    wrapped.add(col)
        return wrapped

    def batch_check_view_data(
        self, view_names: list[str], db_id: int, tenant_id: str
    ) -> dict[str, bool]:
        """One round-trip EXISTS probe across many Reporting views.

        Replaces N sequential per-view existence checks. Falls back to assuming each
        view has data if the batch fails — better to show a stale empty-flag than
        block discovery.
        """
        if not view_names:
            return {}

        # Names come from Metabase metadata, not user input, but escape defensively.
        def quote_ident(n: str) -> str:
            return '"' + n.replace('"', '""') + '"'

        def quote_lit(n: str) -> str:
            return "'" + n.replace("'", "''") + "'"

        result_map: dict[str, bool] = {}
        for i in range(0, len(view_names), self._BATCH_DATA_CHECK_SIZE):
            chunk = view_names[i:i + self._BATCH_DATA_CHECK_SIZE]
            parts = [
                f'SELECT {quote_lit(name)} AS view_name, '
                f'EXISTS (SELECT 1 FROM "Reporting".{quote_ident(name)}) AS has_data'
                for name in chunk
            ]
            sql = "\nUNION ALL\n".join(parts)
            try:
                result = metabase_client.execute_sql(sql, db_id, tenant_id=tenant_id)
                for row in result.get("rows", []):
                    if row and len(row) >= 2:
                        result_map[row[0]] = bool(row[1])
            except Exception as e:
                logger.warning(
                    "discover_views: batched data check failed for chunk starting at %d "
                    "(%s); assuming has_data=True for these",
                    i, e,
                )
                for name in chunk:
                    result_map.setdefault(name, True)

        return result_map
