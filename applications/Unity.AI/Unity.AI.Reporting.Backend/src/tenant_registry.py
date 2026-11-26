"""
Tenant registry: the tenants UAI knows about and their Metabase database and collection.

UGM sends each tenant's Metabase ids in the signed JWT (AB#33978). A valid token
registers (or updates) the tenant here, so code without a token (startup seed-embed,
the embed-all CronJob, /ready) can still list tenants. There is no fallback tenant:
an unknown tenant is an error.
"""
import logging
import threading
import time
from dataclasses import dataclass, field
from typing import Dict, List, Optional

from database import db_manager

logger = logging.getLogger(__name__)

DEFAULT_SCHEMA_TYPES = ["public", "custom"]

# Seconds between background-embed attempts for the same db_id in this process,
# so a burst of requests for a not-yet-embedded tenant starts one embed, not many.
_EMBED_RETRY_SECONDS = 120


class UnknownTenantError(LookupError):
    """The tenant has never presented a token with Metabase ids."""


@dataclass(frozen=True)
class TenantEntry:
    tenant_name: str
    db_id: int
    collection_id: int
    schema_types: List[str] = field(default_factory=lambda: list(DEFAULT_SCHEMA_TYPES))

    def as_config(self) -> Dict:
        """Same shape the old tenant_config.json entries had, for existing callers."""
        return {
            "db_id": self.db_id,
            "collection_id": self.collection_id,
            "schema_types": list(self.schema_types),
        }


def parse_schema_types(value) -> List[str]:
    """Claim value 'public,custom' (or a list) -> ['public', 'custom']."""
    if isinstance(value, (list, tuple)):
        items = [str(v) for v in value]
    else:
        items = str(value or "").split(",")
    result = []
    for item in items:
        item = item.strip().lower()
        if item and item not in result:
            result.append(item)
    return result or list(DEFAULT_SCHEMA_TYPES)


class TenantRegistry:
    """Reads and writes the tenant_registry table, with a per-process cache."""

    def __init__(self, manager=db_manager):
        self._db = manager
        self._cache: Dict[str, TenantEntry] = {}
        self._lock = threading.Lock()
        self._embed_attempts: Dict[int, float] = {}
        self._embedded_db_ids = set()

    # -- writes ---------------------------------------------------------------

    def register(self, entry: TenantEntry) -> bool:
        """Insert or update a tenant. Returns True when the stored values changed."""
        with self._lock:
            if self._cache.get(entry.tenant_name) == entry:
                return False

        with self._db.get_connection() as conn, conn.cursor() as cur:
            cur.execute("""
                    INSERT INTO tenant_registry (tenant_name, db_id, collection_id, schema_types)
                    VALUES (%s, %s, %s, %s)
                    ON CONFLICT (tenant_name) DO UPDATE
                        SET db_id = EXCLUDED.db_id,
                            collection_id = EXCLUDED.collection_id,
                            schema_types = EXCLUDED.schema_types,
                            updated_at = CURRENT_TIMESTAMP
                        WHERE tenant_registry.db_id IS DISTINCT FROM EXCLUDED.db_id
                           OR tenant_registry.collection_id IS DISTINCT FROM EXCLUDED.collection_id
                           OR tenant_registry.schema_types IS DISTINCT FROM EXCLUDED.schema_types
                    RETURNING tenant_name
                """, (entry.tenant_name, entry.db_id, entry.collection_id, list(entry.schema_types)))
            changed = cur.fetchone() is not None
            conn.commit()

        with self._lock:
            self._cache[entry.tenant_name] = entry

        if changed:
            logger.info(
                f"Registered tenant '{entry.tenant_name}': db_id={entry.db_id}, "
                f"collection_id={entry.collection_id}, schema_types={entry.schema_types}"
            )
        return changed

    # -- reads ----------------------------------------------------------------

    def find(self, tenant_name: str) -> Optional[TenantEntry]:
        with self._lock:
            cached = self._cache.get(tenant_name)
        if cached:
            return cached

        with self._db.get_connection() as conn, conn.cursor() as cur:
            cur.execute("""
                    SELECT tenant_name, db_id, collection_id, schema_types
                    FROM tenant_registry WHERE tenant_name = %s
                """, (tenant_name,))
            row = cur.fetchone()
        if not row:
            return None

        entry = TenantEntry(row[0], row[1], row[2], parse_schema_types(row[3]))
        with self._lock:
            self._cache[tenant_name] = entry
        return entry

    def get(self, tenant_name: str) -> TenantEntry:
        entry = self.find(tenant_name)
        if entry is None:
            raise UnknownTenantError(f"Tenant '{tenant_name}' is not registered for AI Reporting.")
        return entry

    def get_config(self, tenant_name: str) -> Dict:
        return self.get(tenant_name).as_config()

    def list_all(self) -> List[TenantEntry]:
        """All registered tenants, read from the database (not the cache)."""
        with self._db.get_connection() as conn, conn.cursor() as cur:
            cur.execute("""
                    SELECT tenant_name, db_id, collection_id, schema_types
                    FROM tenant_registry ORDER BY tenant_name
                """)
            rows = cur.fetchall()
        return [TenantEntry(r[0], r[1], r[2], parse_schema_types(r[3])) for r in rows]

    def find_by_db_id(self, db_id: int) -> Optional[TenantEntry]:
        return next((entry for entry in self.list_all() if entry.db_id == db_id), None)

    # -- first-use embedding --------------------------------------------------

    def is_embedded(self, db_id: int, collection_name: str) -> bool:
        with self._lock:
            if db_id in self._embedded_db_ids:
                return True
        embedded = db_id in self._db.has_embeddings_for_db_ids({db_id}, collection_name)
        if embedded:
            # embed_schemas adds new embeddings before deleting old ones, so once a
            # db has embeddings it keeps a usable set.
            with self._lock:
                self._embedded_db_ids.add(db_id)
        return embedded

    def ensure_embedding_started(self, entry: TenantEntry, collection_name: str) -> bool:
        """Start a background embed when the tenant's db has no embeddings yet.

        Returns True when the db is already embedded. embed_schemas takes the
        per-db advisory lock, so other workers/pods doing the same thing skip.
        """
        if self.is_embedded(entry.db_id, collection_name):
            return True

        now = time.monotonic()
        with self._lock:
            last = self._embed_attempts.get(entry.db_id)
            if last is not None and now - last < _EMBED_RETRY_SECONDS:
                return False
            self._embed_attempts[entry.db_id] = now

        def _embed():
            try:
                # Imported here: embeddings imports this module indirectly.
                from embeddings import embedding_manager
                logger.info(f"First-use embed for tenant '{entry.tenant_name}' (db_id={entry.db_id})")
                embedding_manager.embed_schemas(entry.db_id, list(entry.schema_types), tenant_id=entry.tenant_name)
                # embed_schemas returns quietly when another run holds the db's lock.
                if self.is_embedded(entry.db_id, collection_name):
                    logger.info(f"First-use embed finished for db_id={entry.db_id}")
                else:
                    logger.info(f"First-use embed for db_id={entry.db_id} skipped; another embed is running")
            except Exception as e:
                logger.warning(f"First-use embed failed for db_id={entry.db_id}: {e}", exc_info=True)

        threading.Thread(target=_embed, daemon=True, name=f"first-use-embed-{entry.db_id}").start()
        return False


tenant_registry = TenantRegistry()
