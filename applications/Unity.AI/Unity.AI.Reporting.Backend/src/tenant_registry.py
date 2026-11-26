"""
Tenant registry: the tenants UAI knows about and their Metabase database and collection.

UGM sends each tenant's Metabase ids in the signed JWT (AB#33978), and AI Setup's AI Reporting
models and API version (AB#35030). A valid token registers (or updates) the tenant here, so code
without a token (startup seed-embed, the embed-all CronJob, /ready) can still list tenants and
use the right models. There is no fallback tenant and no default model: an unknown tenant, or
one registered before AI Setup's models were sent, is an error for token-less work.
"""
import logging
import threading
import time
from dataclasses import dataclass, field
from typing import Dict, List, Optional

import ai_models
from database import db_manager

logger = logging.getLogger(__name__)

DEFAULT_SCHEMA_TYPES = ["public", "custom"]

# Seconds between background-embed attempts for the same db_id in this process,
# so a burst of requests for a not-yet-embedded tenant starts one embed, not many.
_EMBED_RETRY_SECONDS = 120

# The embedding deployment every vector made before AB#35030 came from (UAI had it in code
# then, and recorded no model with the vectors). Not a default: AI Setup picks the model.
LEGACY_EMBEDDING_DEPLOYMENT = "text-embedding-3-large"


class UnknownTenantError(LookupError):
    """The tenant has never presented a token with Metabase ids."""


@dataclass(frozen=True)
class TenantEntry:
    tenant_name: str
    db_id: int
    collection_id: int
    schema_types: List[str] = field(default_factory=lambda: list(DEFAULT_SCHEMA_TYPES))
    # AI Setup's AI Reporting models, each with its API version (from the token); None for a
    # row registered before AB#35030.
    chat_deployment: Optional[str] = None
    chat_api_version: Optional[str] = None
    embedding_deployment: Optional[str] = None
    embedding_api_version: Optional[str] = None

    @property
    def models(self) -> Optional[ai_models.AIModels]:
        try:
            return ai_models.parse(self.chat_deployment, self.chat_api_version,
                                   self.embedding_deployment, self.embedding_api_version)
        except ValueError:
            return None

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
                    INSERT INTO tenant_registry (tenant_name, db_id, collection_id, schema_types,
                                                 chat_deployment, chat_api_version,
                                                 embedding_deployment, embedding_api_version)
                    VALUES (%s, %s, %s, %s, %s, %s, %s, %s)
                    ON CONFLICT (tenant_name) DO UPDATE
                        SET db_id = EXCLUDED.db_id,
                            collection_id = EXCLUDED.collection_id,
                            schema_types = EXCLUDED.schema_types,
                            chat_deployment = EXCLUDED.chat_deployment,
                            chat_api_version = EXCLUDED.chat_api_version,
                            embedding_deployment = EXCLUDED.embedding_deployment,
                            embedding_api_version = EXCLUDED.embedding_api_version,
                            updated_at = CURRENT_TIMESTAMP
                        WHERE tenant_registry.db_id IS DISTINCT FROM EXCLUDED.db_id
                           OR tenant_registry.collection_id IS DISTINCT FROM EXCLUDED.collection_id
                           OR tenant_registry.schema_types IS DISTINCT FROM EXCLUDED.schema_types
                           OR tenant_registry.chat_deployment IS DISTINCT FROM EXCLUDED.chat_deployment
                           OR tenant_registry.chat_api_version IS DISTINCT FROM EXCLUDED.chat_api_version
                           OR tenant_registry.embedding_deployment IS DISTINCT FROM EXCLUDED.embedding_deployment
                           OR tenant_registry.embedding_api_version IS DISTINCT FROM EXCLUDED.embedding_api_version
                    RETURNING tenant_name
                """, (entry.tenant_name, entry.db_id, entry.collection_id, list(entry.schema_types),
                      entry.chat_deployment, entry.chat_api_version,
                      entry.embedding_deployment, entry.embedding_api_version))
            changed = cur.fetchone() is not None
            conn.commit()

        with self._lock:
            self._cache[entry.tenant_name] = entry

        if changed:
            logger.info(
                f"Registered tenant '{entry.tenant_name}': db_id={entry.db_id}, "
                f"collection_id={entry.collection_id}, schema_types={entry.schema_types}, "
                f"chat={entry.chat_deployment} ({entry.chat_api_version}), "
                f"embedding={entry.embedding_deployment} ({entry.embedding_api_version})"
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
                    SELECT tenant_name, db_id, collection_id, schema_types,
                           chat_deployment, chat_api_version, embedding_deployment, embedding_api_version
                    FROM tenant_registry WHERE tenant_name = %s
                """, (tenant_name,))
            row = cur.fetchone()
        if not row:
            return None

        entry = _entry_from_row(row)
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
                    SELECT tenant_name, db_id, collection_id, schema_types,
                           chat_deployment, chat_api_version, embedding_deployment, embedding_api_version
                    FROM tenant_registry ORDER BY tenant_name
                """)
            rows = cur.fetchall()
        return [_entry_from_row(r) for r in rows]

    def find_by_db_id(self, db_id: int) -> Optional[TenantEntry]:
        return next((entry for entry in self.list_all() if entry.db_id == db_id), None)

    # -- first-use embedding --------------------------------------------------

    def get_embedded_with(self, db_id: int) -> str:
        """The embedding deployment the db's vectors were made with.

        Vectors from before AB#35030 recorded none; they were all made with the deployment UAI
        then had in code, so an unrecorded db counts as LEGACY_EMBEDDING_DEPLOYMENT."""
        with self._db.get_connection() as conn, conn.cursor() as cur:
            cur.execute("""
                    SELECT embedded_with FROM tenant_registry
                    WHERE db_id = %s AND embedded_with IS NOT NULL
                    LIMIT 1
                """, (db_id,))
            row = cur.fetchone()
        return row[0] if row else LEGACY_EMBEDDING_DEPLOYMENT

    def mark_embedded(self, db_id: int, embedding_deployment: str) -> None:
        """Record which embedding deployment the db's vectors now come from."""
        with self._db.get_connection() as conn, conn.cursor() as cur:
            cur.execute(
                "UPDATE tenant_registry SET embedded_with = %s WHERE db_id = %s",
                (embedding_deployment, db_id))
            conn.commit()
        with self._lock:
            # The db's vectors now belong to this model only.
            self._embedded_db_ids = {pair for pair in self._embedded_db_ids if pair[0] != db_id}
            self._embedded_db_ids.add((db_id, embedding_deployment))

    def is_embedded(self, db_id: int, collection_name: str, embedding_deployment: str) -> bool:
        """True when the db has embeddings made with this embedding deployment."""
        with self._lock:
            if (db_id, embedding_deployment) in self._embedded_db_ids:
                return True
        if db_id not in self._db.has_embeddings_for_db_ids({db_id}, collection_name):
            return False

        if self.get_embedded_with(db_id) != embedding_deployment:
            return False

        # embed_schemas adds new embeddings before deleting old ones, so once a db has
        # embeddings it keeps a usable set.
        with self._lock:
            self._embedded_db_ids.add((db_id, embedding_deployment))
        return True

    def ensure_embedding_started(self, entry: TenantEntry, collection_name: str) -> bool:
        """Start a background embed when the tenant's db has no embeddings made with
        AI Setup's current embedding model (first use, or the model was changed).

        Returns True when the db is already embedded with that model. embed_schemas takes
        the per-db advisory lock, so other workers/pods doing the same thing skip.
        """
        models = entry.models
        if models is None:
            raise ai_models.AIModelsNotSetError(
                f"Tenant '{entry.tenant_name}' has no AI Reporting models from AI Setup.")

        if self.is_embedded(entry.db_id, collection_name, models.embedding_deployment):
            return True

        # Embeddings from another model: every vector must be redone, and cached answers
        # matched with the old model's question vectors are dropped.
        model_changed = self.get_embedded_with(entry.db_id) != models.embedding_deployment

        now = time.monotonic()
        with self._lock:
            last = self._embed_attempts.get(entry.db_id)
            if last is not None and now - last < _EMBED_RETRY_SECONDS:
                return False
            self._embed_attempts[entry.db_id] = now

        def _embed():
            try:
                # Imported here: embeddings imports this module indirectly.
                from database import cache_repository
                from embeddings import embedding_manager
                with ai_models.use(models):
                    reason = "embedding model changed" if model_changed else "first use"
                    logger.info(f"Embedding tenant '{entry.tenant_name}' (db_id={entry.db_id}, "
                                f"{reason}, model={models.embedding_deployment})")
                    if model_changed:
                        purged = cache_repository.purge_db(entry.db_id)
                        logger.info(f"Dropped {purged} cached answers for db_id={entry.db_id}")
                    completed = embedding_manager.embed_schemas(
                        entry.db_id, list(entry.schema_types), tenant_id=entry.tenant_name, force=model_changed)
                    # True only when the db's vectors are complete with this model; False when
                    # another run held the lock or extraction was partial (retried later).
                    if completed:
                        self.mark_embedded(entry.db_id, models.embedding_deployment)
                        logger.info(f"Embed finished for db_id={entry.db_id}")
                    else:
                        logger.info(f"Embed for db_id={entry.db_id} not completed "
                                    "(another embed is running, or extraction was partial)")
            except Exception as e:
                logger.warning(f"Embed failed for db_id={entry.db_id}: {e}", exc_info=True)

        threading.Thread(target=_embed, daemon=True, name=f"first-use-embed-{entry.db_id}").start()
        return False


def _entry_from_row(row) -> TenantEntry:
    return TenantEntry(
        tenant_name=row[0],
        db_id=row[1],
        collection_id=row[2],
        schema_types=parse_schema_types(row[3]),
        chat_deployment=row[4],
        chat_api_version=row[5],
        embedding_deployment=row[6],
        embedding_api_version=row[7],
    )


tenant_registry = TenantRegistry()
