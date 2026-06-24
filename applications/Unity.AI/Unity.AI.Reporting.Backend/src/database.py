"""
Database module for managing PostgreSQL connections and operations.
"""
import psycopg
import logging
import zlib
from contextlib import contextmanager
from typing import Any, List, Dict, Optional
import json
from config import config

# Configure logging
logger = logging.getLogger(__name__)


class DatabaseManager:
    """Manages database connections and operations"""
    
    def __init__(self):
        self.config = config.database
    
    def get_connection(self):
        """Get a database connection"""
        return psycopg.connect(
            host=self.config.host,
            port=self.config.port,
            dbname=self.config.name,
            user=self.config.user,
            password=self.config.password
        )
    
    def init_tables(self):
        """Initialize all required database tables"""
        with self.get_connection() as conn:
            with conn.cursor() as cur:
                # Chat table for conversation history
                cur.execute("""
                    CREATE TABLE IF NOT EXISTS chats (
                        chat_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                        user_id TEXT NOT NULL,
                        title TEXT NOT NULL,
                        conversation JSONB NOT NULL,
                        tenant_id TEXT,
                        metabase_url TEXT,
                        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    );
                    
                    CREATE INDEX IF NOT EXISTS idx_chats_user_id ON chats(user_id);
                    CREATE INDEX IF NOT EXISTS idx_chats_tenant_id ON chats(tenant_id);
                """)
                
                # Feedback table for bug reports and user feedback
                cur.execute("""
                    CREATE TABLE IF NOT EXISTS feedback (
                        feedback_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                        chat_id UUID NOT NULL REFERENCES chats(chat_id) ON DELETE CASCADE,
                        user_id TEXT NOT NULL,
                        tenant_id TEXT NOT NULL,
                        feedback_type TEXT NOT NULL DEFAULT 'bug_report',
                        message TEXT,
                        user_agent TEXT,
                        metadata JSONB,
                        status TEXT DEFAULT 'open',
                        current_question TEXT,
                        current_sql TEXT,
                        current_sql_explanation TEXT,
                        previous_question TEXT,
                        previous_sql TEXT,
                        previous_sql_explanation TEXT,
                        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    );
                    
                    CREATE INDEX IF NOT EXISTS idx_feedback_chat_id ON feedback(chat_id);
                    CREATE INDEX IF NOT EXISTS idx_feedback_user_id ON feedback(user_id);
                    CREATE INDEX IF NOT EXISTS idx_feedback_tenant_id ON feedback(tenant_id);
                    CREATE INDEX IF NOT EXISTS idx_feedback_type ON feedback(feedback_type);
                    CREATE INDEX IF NOT EXISTS idx_feedback_status ON feedback(status);
                """)
                
                # Semantic query cache table
                cur.execute("""
                    CREATE TABLE IF NOT EXISTS query_cache (
                        cache_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                        tenant_id TEXT NOT NULL,
                        db_id INTEGER NOT NULL,
                        schema_fingerprint TEXT NOT NULL,
                        query_text TEXT NOT NULL,
                        normalized_query TEXT NOT NULL,
                        query_embedding vector(3072) NOT NULL,
                        response_payload JSONB NOT NULL,
                        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        accessed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        access_count INTEGER DEFAULT 1
                    );

                    CREATE UNIQUE INDEX IF NOT EXISTS idx_query_cache_exact
                        ON query_cache(tenant_id, db_id, schema_fingerprint, normalized_query);
                    CREATE INDEX IF NOT EXISTS idx_query_cache_tenant_db
                        ON query_cache(tenant_id, db_id, schema_fingerprint);
                """)

                # Schema version tracking — drives conditional semantic-cache invalidation.
                # One row per (db_id, collection_name) holds the structural fingerprint of
                # the embedded schema; embed_schemas compares + purges on change.
                cur.execute("""
                    CREATE TABLE IF NOT EXISTS schema_versions (
                        db_id           INTEGER NOT NULL,
                        collection_name TEXT    NOT NULL,
                        fingerprint     TEXT    NOT NULL,
                        updated_at      TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        PRIMARY KEY (db_id, collection_name)
                    );
                """)

                # One-time migration: pre-fix cache rows pre-date the structural fingerprint
                # and may encode SQL against a since-changed schema. Purge them once on
                # first init after the fix lands; guarded by a sentinel row in
                # schema_versions so it never repeats.
                cur.execute("""
                    INSERT INTO schema_versions (db_id, collection_name, fingerprint)
                    VALUES (0, '__migration_v1__', 'done')
                    ON CONFLICT (db_id, collection_name) DO NOTHING
                    RETURNING db_id
                """)
                if cur.fetchone():
                    cur.execute("DELETE FROM query_cache")
                    logger.info(
                        f"One-time cache migration: purged {cur.rowcount} pre-fix query_cache entries"
                    )

                # ivfflat index requires rows to exist first — created separately via evict_old
                # or on first similarity search. Skip here to avoid error on empty table.

                conn.commit()
    
    def purge_embeddings(self, db_id: Optional[int] = None, collection_name: str = "embedded_schema"):
        """
        Delete existing embeddings from the vector store.

        Args:
            db_id: Optional database ID to filter by
            collection_name: Name of the collection to purge
        """
        if db_id:
            logger.info(f"Purging existing embeddings for db_id: {db_id}...")
        else:
            logger.info("Purging all existing embeddings...")

        try:
            with self.get_connection() as conn:
                with conn.cursor() as cur:
                    if db_id:
                        # Delete only embeddings for specific db_id
                        cur.execute("""
                            DELETE FROM langchain_pg_embedding
                            WHERE collection_id IN (
                                SELECT uuid FROM langchain_pg_collection
                                WHERE name = %s
                            )
                            AND cmetadata->>'db_id' = %s
                        """, (collection_name, str(db_id)))
                    else:
                        # Delete all embeddings
                        cur.execute("""
                            DELETE FROM langchain_pg_embedding
                            WHERE collection_id IN (
                                SELECT uuid FROM langchain_pg_collection
                                WHERE name = %s
                            )
                        """, (collection_name,))

                    deleted_count = cur.rowcount
                    conn.commit()
                    logger.info(f"Purged {deleted_count} existing embeddings")
        except Exception as e:
            logger.exception(f"Error purging embeddings: {e}")
            raise

    def get_embedding_ids(self, db_id: int,
                          collection_name: str = "embedded_schema") -> List[str]:
        """Return the ids of all embeddings for a given (db_id, collection_name).

        Used by embed_schemas to capture the existing row set before adding
        fresh embeddings, so the old rows can be deleted *after* the new ones
        are inserted (atomic-ish swap — the running app always sees a complete
        embedding set).
        """
        with self.get_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("""
                    SELECT id FROM langchain_pg_embedding
                    WHERE collection_id IN (
                        SELECT uuid FROM langchain_pg_collection
                        WHERE name = %s
                    )
                    AND cmetadata->>'db_id' = %s
                """, (collection_name, str(db_id)))
                return [str(row[0]) for row in cur.fetchall()]

    def purge_embeddings_by_ids(self, ids: List[str],
                                collection_name: str = "embedded_schema") -> int:
        """Delete embeddings with the given ids (scoped to a collection for safety).

        Returns the number of rows actually deleted. No-op if `ids` is empty.
        """
        if not ids:
            return 0
        with self.get_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("""
                    DELETE FROM langchain_pg_embedding
                    WHERE id = ANY(%s)
                    AND collection_id IN (
                        SELECT uuid FROM langchain_pg_collection
                        WHERE name = %s
                    )
                """, (ids, collection_name))
                deleted = cur.rowcount
                conn.commit()
                logger.info(f"Purged {deleted} stale embeddings by id")
                return deleted

    @contextmanager
    def embed_lock(self, db_id: int, collection_name: str = "embedded_schema"):
        """Advisory lock serializing the embed swap per (db_id, collection_name).

        Stops two overlapping embed runs from racing the add-then-delete swap
        and leaving duplicate embeddings. Yields True if the caller owns the
        swap, or False if another run holds it (caller should skip). Released on
        exit, or when the connection closes if unlock is skipped.
        """
        # Two int4 keys: a namespace from the collection name + the db_id.
        # crc32 is unsigned; shift into Postgres's signed int4 range.
        key1 = zlib.crc32(collection_name.encode("utf-8")) - 2**31
        key2 = db_id
        conn = self.get_connection()
        try:
            with conn.cursor() as cur:
                cur.execute("SELECT pg_try_advisory_lock(%s, %s)", (key1, key2))
                acquired = cur.fetchone()[0]
            conn.commit()
            if not acquired:
                yield False
                return
            try:
                yield True
            finally:
                with conn.cursor() as cur:
                    cur.execute("SELECT pg_advisory_unlock(%s, %s)", (key1, key2))
                conn.commit()
        finally:
            conn.close()


class ChatRepository:
    """Repository for chat/conversation management"""
    
    def __init__(self, db_manager: DatabaseManager):
        self.db = db_manager
    
    def get_user_chats(self, user_id: str, tenant_id: str) -> List[Dict[str, Any]]:
        """Get all chats for a user and tenant"""
        with self.db.get_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("""
                    SELECT chat_id, title, created_at, updated_at 
                    FROM chats 
                    WHERE user_id = %s AND tenant_id = %s 
                    ORDER BY updated_at DESC
                """, (user_id, tenant_id))
                
                chats = []
                for row in cur.fetchall():
                    chats.append({
                        "id": str(row[0]),
                        "title": row[1],
                        "created_at": row[2].isoformat(),
                        "updated_at": row[3].isoformat()
                    })
                
                return chats
    
    def get_chat(self, chat_id: str, user_id: str) -> Optional[Dict[str, Any]]:
        """Get a specific chat"""
        with self.db.get_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("""
                    SELECT conversation, metabase_url, tenant_id
                    FROM chats 
                    WHERE chat_id = %s AND user_id = %s
                """, (chat_id, user_id))
                
                row = cur.fetchone()
                if not row:
                    return None
                
                return {
                    "conversation": row[0],
                    "metabase_url": row[1],
                    "tenant_id": row[2]
                }
    
    def save_chat(self, user_id: str, tenant_id: str, metabase_url: str, 
                  title: str, conversation: List[Dict], chat_id: Optional[str] = None) -> str:
        """Save or update a chat"""
        with self.db.get_connection() as conn:
            with conn.cursor() as cur:
                if chat_id:
                    # Update existing chat
                    cur.execute("""
                        UPDATE chats 
                        SET title = %s, conversation = %s, updated_at = CURRENT_TIMESTAMP
                        WHERE chat_id = %s AND user_id = %s
                        RETURNING chat_id
                    """, (title, json.dumps(conversation), chat_id, user_id))
                    
                    row = cur.fetchone()
                    if not row:
                        raise ValueError("Chat not found")
                    
                    result_chat_id = str(row[0])
                else:
                    # Create new chat
                    cur.execute("""
                        INSERT INTO chats (user_id, tenant_id, metabase_url, title, conversation)
                        VALUES (%s, %s, %s, %s, %s)
                        RETURNING chat_id
                    """, (user_id, tenant_id, metabase_url, title, json.dumps(conversation)))
                    
                    row = cur.fetchone()
                    if not row:
                        raise ValueError("Failed to create chat")
                    result_chat_id = str(row[0])
                
                conn.commit()
                return result_chat_id
    
    def delete_chat(self, chat_id: str, user_id: str) -> bool:
        """Delete a chat"""
        with self.db.get_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("""
                    DELETE FROM chats 
                    WHERE chat_id = %s AND user_id = %s
                """, (chat_id, user_id))
                
                deleted = cur.rowcount > 0
                conn.commit()
                return deleted
    
    def update_chat_cards(self, chat_id: str, user_id: str, conversation: List[Dict]):
        """Update card IDs in a chat conversation"""
        with self.db.get_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("""
                    UPDATE chats 
                    SET conversation = %s, updated_at = CURRENT_TIMESTAMP
                    WHERE chat_id = %s AND user_id = %s
                """, (json.dumps(conversation), chat_id, user_id))
                conn.commit()


class FeedbackRepository:
    """Repository for feedback/bug reports management"""
    
    def __init__(self, db_manager: DatabaseManager):
        self.db = db_manager
    
    def submit_feedback(self, chat_id: str, user_id: str, tenant_id: str,
                       feedback_type: str, message: str, user_agent: Optional[str] = None,
                       metadata: Optional[Dict] = None, current_question: Optional[str] = None,
                       current_sql: Optional[str] = None, current_sql_explanation: Optional[str] = None,
                       previous_question: Optional[str] = None, previous_sql: Optional[str] = None,
                       previous_sql_explanation: Optional[str] = None) -> str:
        """Submit feedback for a chat"""
        with self.db.get_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("""
                    INSERT INTO feedback (chat_id, user_id, tenant_id, feedback_type, message, user_agent, metadata,
                                        current_question, current_sql, current_sql_explanation,
                                        previous_question, previous_sql, previous_sql_explanation)
                    VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s, %s)
                    RETURNING feedback_id
                """, (chat_id, user_id, tenant_id, feedback_type, message, user_agent,
                      json.dumps(metadata) if metadata else None,
                      current_question, current_sql, current_sql_explanation,
                      previous_question, previous_sql, previous_sql_explanation))
                
                row = cur.fetchone()
                if not row:
                    raise ValueError("Failed to submit feedback")
                feedback_id = str(row[0])
                conn.commit()
                return feedback_id
    
    def get_feedback(self, feedback_id: str) -> Optional[Dict[str, Any]]:
        """Get a specific feedback entry"""
        with self.db.get_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("""
                    SELECT f.feedback_id, f.chat_id, f.user_id, f.tenant_id, f.feedback_type,
                           f.message, f.user_agent, f.metadata, f.status, f.created_at, f.updated_at,
                           f.current_question, f.current_sql, f.current_sql_explanation,
                           f.previous_question, f.previous_sql, f.previous_sql_explanation,
                           c.title as chat_title
                    FROM feedback f
                    LEFT JOIN chats c ON f.chat_id = c.chat_id
                    WHERE f.feedback_id = %s
                """, (feedback_id,))
                
                row = cur.fetchone()
                if not row:
                    return None
                
                return {
                    "feedback_id": str(row[0]),
                    "chat_id": str(row[1]),
                    "user_id": row[2],
                    "tenant_id": row[3],
                    "feedback_type": row[4],
                    "message": row[5],
                    "user_agent": row[6],
                    "metadata": row[7],
                    "status": row[8],
                    "created_at": row[9].isoformat(),
                    "updated_at": row[10].isoformat(),
                    "current_question": row[11],
                    "current_sql": row[12],
                    "current_sql_explanation": row[13],
                    "previous_question": row[14],
                    "previous_sql": row[15],
                    "previous_sql_explanation": row[16],
                    "chat_title": row[17]
                }
    
    def get_feedback_by_chat(self, chat_id: str) -> List[Dict[str, Any]]:
        """Get all feedback for a specific chat"""
        with self.db.get_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("""
                    SELECT feedback_id, feedback_type, message, status, created_at
                    FROM feedback
                    WHERE chat_id = %s
                    ORDER BY created_at DESC
                """, (chat_id,))
                
                feedback_list = []
                for row in cur.fetchall():
                    feedback_list.append({
                        "feedback_id": str(row[0]),
                        "feedback_type": row[1],
                        "message": row[2],
                        "status": row[3],
                        "created_at": row[4].isoformat()
                    })
                
                return feedback_list
    
    def update_feedback_status(self, feedback_id: str, status: str) -> bool:
        """Update feedback status"""
        with self.db.get_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("""
                    UPDATE feedback
                    SET status = %s, updated_at = CURRENT_TIMESTAMP
                    WHERE feedback_id = %s
                """, (status, feedback_id))

                updated = cur.rowcount > 0
                conn.commit()
                return updated

    def get_all_feedback(self, limit: int = 100, offset: int = 0) -> List[Dict[str, Any]]:
        """Get all feedback entries for admin view"""
        with self.db.get_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("""
                    SELECT f.feedback_id, f.chat_id, f.user_id, f.tenant_id, f.feedback_type,
                           f.message, f.user_agent, f.metadata, f.status, f.created_at, f.updated_at,
                           f.current_question, f.current_sql, f.current_sql_explanation,
                           f.previous_question, f.previous_sql, f.previous_sql_explanation,
                           c.title as chat_title,
                           c.conversation->0->'embed'->'tokens' as tokens
                    FROM feedback f
                    LEFT JOIN chats c ON f.chat_id = c.chat_id
                    ORDER BY f.created_at DESC
                    LIMIT %s OFFSET %s
                """, (limit, offset))

                feedback_list = []
                for row in cur.fetchall():
                    feedback_item = {
                        "feedback_id": str(row[0]),
                        "chat_id": str(row[1]),
                        "user_id": row[2],
                        "tenant_id": row[3],
                        "feedback_type": row[4],
                        "message": row[5],
                        "user_agent": row[6],
                        "metadata": row[7],
                        "status": row[8],
                        "created_at": row[9].isoformat(),
                        "updated_at": row[10].isoformat(),
                        "current_question": row[11],
                        "current_sql": row[12],
                        "current_sql_explanation": row[13],
                        "previous_question": row[14],
                        "previous_sql": row[15],
                        "previous_sql_explanation": row[16],
                        "chat_title": row[17]
                    }

                    # Add token information if available
                    if row[18]:
                        feedback_item["tokens"] = row[18]

                    feedback_list.append(feedback_item)

                return feedback_list


class CacheRepository:
    """Repository for semantic query cache"""

    def __init__(self, db_manager: DatabaseManager):
        self.db = db_manager

    @staticmethod
    def build_fingerprint(db_id: int, schema_types: list, collection_name: str) -> str:
        """Build a schema fingerprint string for cache scoping."""
        return f"{db_id}:{':'.join(sorted(schema_types))}:{collection_name}"

    def find_exact(self, tenant_id: str, db_id: int, schema_types: list,
                   collection_name: str, normalized_query: str) -> Optional[Dict[str, Any]]:
        """Layer 1: exact normalized-query match — no embedding cost."""
        fp = self.build_fingerprint(db_id, schema_types, collection_name)
        with self.db.get_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("""
                    SELECT cache_id, response_payload
                    FROM query_cache
                    WHERE tenant_id = %s
                      AND db_id = %s
                      AND schema_fingerprint = %s
                      AND normalized_query = %s
                    LIMIT 1
                """, (tenant_id, db_id, fp, normalized_query))
                row = cur.fetchone()
                if row:
                    return {
                        "cache_id": str(row[0]),
                        "response_payload": row[1],
                        "similarity": 1.0
                    }
        return None

    def find_similar(self, tenant_id: str, db_id: int, schema_types: list,
                     collection_name: str, embedding: list,
                     threshold: float) -> Optional[Dict[str, Any]]:
        """Layer 2: cosine similarity search via pgvector."""
        fp = self.build_fingerprint(db_id, schema_types, collection_name)
        embedding_str = "[" + ",".join(str(v) for v in embedding) + "]"
        with self.db.get_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("SET hnsw.ef_search = 64")
                cur.execute("""
                    SELECT cache_id, response_payload,
                           1 - (query_embedding <=> %s::vector) AS similarity
                    FROM query_cache
                    WHERE tenant_id = %s
                      AND db_id = %s
                      AND schema_fingerprint = %s
                      AND 1 - (query_embedding <=> %s::vector) >= %s
                    ORDER BY query_embedding <=> %s::vector
                    LIMIT 1
                """, (embedding_str, tenant_id, db_id, fp,
                      embedding_str, threshold, embedding_str))
                row = cur.fetchone()
                if row:
                    return {
                        "cache_id": str(row[0]),
                        "response_payload": row[1],
                        "similarity": float(row[2])
                    }
        return None

    def find_similar_topk(
        self, tenant_id: str, db_id: int, schema_types: list,
        collection_name: str, embedding: list,
        threshold: float, k: int = 5
    ) -> list:
        """Top-K cosine similarity search with floor = threshold.
        Returns list sorted by similarity DESC (closest first).
        Each dict: cache_id, response_payload, query_text, similarity."""
        fp = self.build_fingerprint(db_id, schema_types, collection_name)
        embedding_str = "[" + ",".join(str(v) for v in embedding) + "]"
        with self.db.get_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("SET hnsw.ef_search = 64")
                cur.execute("""
                    SELECT cache_id, response_payload, query_text,
                           1 - (query_embedding <=> %s::vector) AS similarity
                    FROM query_cache
                    WHERE tenant_id = %s
                      AND db_id = %s
                      AND schema_fingerprint = %s
                      AND 1 - (query_embedding <=> %s::vector) >= %s
                    ORDER BY query_embedding <=> %s::vector
                    LIMIT %s
                """, (embedding_str, tenant_id, db_id, fp,
                      embedding_str, threshold, embedding_str, k))
                return [
                    {
                        "cache_id": str(row[0]),
                        "response_payload": row[1],
                        "query_text": row[2],
                        "similarity": float(row[3]),
                    }
                    for row in cur.fetchall()
                ]

    def get_recent_normalized_queries(
        self, tenant_id: str, db_id: int, schema_types: list,
        collection_name: str, limit: int = 200
    ) -> list:
        """Fetch recent normalized queries for fuzzy matching.
        Returns list of {"normalized_query": str, "cache_id": str} ordered by accessed_at DESC."""
        fp = self.build_fingerprint(db_id, schema_types, collection_name)
        with self.db.get_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("""
                    SELECT normalized_query, cache_id
                    FROM query_cache
                    WHERE tenant_id = %s
                      AND db_id = %s
                      AND schema_fingerprint = %s
                    ORDER BY accessed_at DESC
                    LIMIT %s
                """, (tenant_id, db_id, fp, limit))
                return [
                    {"normalized_query": row[0], "cache_id": str(row[1])}
                    for row in cur.fetchall()
                ]

    def save(self, tenant_id: str, db_id: int, schema_types: list, collection_name: str,
             query_text: str, normalized_query: str, embedding: list,
             response_payload: Dict[str, Any]):
        """Store a new cache entry, updating if the normalized query already exists."""
        fp = self.build_fingerprint(db_id, schema_types, collection_name)
        embedding_str = "[" + ",".join(str(v) for v in embedding) + "]"
        with self.db.get_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("""
                    INSERT INTO query_cache
                        (tenant_id, db_id, schema_fingerprint, query_text, normalized_query,
                         query_embedding, response_payload)
                    VALUES (%s, %s, %s, %s, %s, %s::vector, %s)
                    ON CONFLICT (tenant_id, db_id, schema_fingerprint, normalized_query)
                    DO UPDATE SET
                        response_payload = EXCLUDED.response_payload,
                        query_embedding  = EXCLUDED.query_embedding,
                        accessed_at      = NOW(),
                        access_count     = query_cache.access_count + 1
                """, (tenant_id, db_id, fp, query_text, normalized_query,
                      embedding_str, json.dumps(response_payload)))
                conn.commit()

    def touch(self, cache_id: str):
        """Update access timestamp and increment hit counter."""
        with self.db.get_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("""
                    UPDATE query_cache
                    SET accessed_at  = NOW(),
                        access_count = access_count + 1
                    WHERE cache_id = %s
                """, (cache_id,))
                conn.commit()

    def evict_old(self, days: int = 30) -> int:
        """Delete cache entries older than `days` days. Returns count deleted."""
        with self.db.get_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("""
                    DELETE FROM query_cache
                    WHERE accessed_at < NOW() - %s * INTERVAL '1 day'
                """, (days,))
                deleted = cur.rowcount
                conn.commit()
                return deleted

    def get_schema_fingerprint(self, db_id: int,
                               collection_name: str) -> Optional[str]:
        """Return the stored structural fingerprint for (db_id, collection_name), or None.

        Used by the fingerprint-first fast-path in embed_schemas to skip the
        full re-embed (sample fetches + Azure embedding calls + DB writes)
        when the schema hasn't structurally changed since the last run.
        """
        with self.db.get_connection() as conn:
            with conn.cursor() as cur:
                cur.execute(
                    "SELECT fingerprint FROM schema_versions "
                    "WHERE db_id = %s AND collection_name = %s",
                    (db_id, collection_name),
                )
                row = cur.fetchone()
                return row[0] if row else None

    def update_schema_fingerprint(self, db_id: int, collection_name: str,
                                  fingerprint: str) -> int:
        """Compare new structural fingerprint to the stored one for (db_id, collection_name).
        If it differs, purge that db's query_cache. Always upsert the new fingerprint.

        SELECT ... FOR UPDATE serializes against a concurrent embed-all run
        (e.g. manual CLI vs. server startup).

        Returns the number of query_cache rows purged (0 on first run or unchanged).
        """
        with self.db.get_connection() as conn:
            with conn.cursor() as cur:
                cur.execute(
                    "SELECT fingerprint FROM schema_versions "
                    "WHERE db_id = %s AND collection_name = %s FOR UPDATE",
                    (db_id, collection_name),
                )
                row = cur.fetchone()
                old = row[0] if row else None

                purged = 0
                # Only purge when we had a previous fingerprint and it differs.
                # First-ever embed (old=None) has nothing valid to invalidate.
                if old is not None and old != fingerprint:
                    cur.execute("DELETE FROM query_cache WHERE db_id = %s", (db_id,))
                    purged = cur.rowcount

                cur.execute("""
                    INSERT INTO schema_versions (db_id, collection_name, fingerprint, updated_at)
                    VALUES (%s, %s, %s, NOW())
                    ON CONFLICT (db_id, collection_name)
                    DO UPDATE SET fingerprint = EXCLUDED.fingerprint, updated_at = NOW()
                """, (db_id, collection_name, fingerprint))

                conn.commit()

                if old is None:
                    logger.info(
                        f"Schema fingerprint initialized for db_id={db_id}: {fingerprint}"
                    )
                elif old == fingerprint:
                    logger.info(
                        f"Schema unchanged for db_id={db_id} (fingerprint={fingerprint}); cache retained"
                    )
                else:
                    logger.info(
                        f"Schema changed for db_id={db_id}; "
                        f"purged {purged} stale cache entries (old={old} new={fingerprint})"
                    )
                return purged

    def ensure_hnsw_index(self):
        """Create the hnsw index once the table has rows. hnsw supports up to 16000 dimensions,
        unlike ivfflat which caps at 2000 — required for text-embedding-3-large (3072-d)."""
        with self.db.get_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("SELECT COUNT(*) FROM query_cache")
                count = cur.fetchone()[0]
                if count > 0:
                    cur.execute("""
                        CREATE INDEX IF NOT EXISTS idx_query_cache_embedding
                            ON query_cache
                            USING hnsw (query_embedding vector_cosine_ops)
                            WITH (m = 16, ef_construction = 64)
                    """)
                    conn.commit()


# Global instances
db_manager = DatabaseManager()
chat_repository = ChatRepository(db_manager)
feedback_repository = FeedbackRepository(db_manager)
cache_repository = CacheRepository(db_manager)