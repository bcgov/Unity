"""
Database module for managing PostgreSQL connections and operations.
"""
import psycopg
from typing import Any, List, Dict, Optional
import json
from config import config


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
                        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                    );
                    
                    CREATE INDEX IF NOT EXISTS idx_feedback_chat_id ON feedback(chat_id);
                    CREATE INDEX IF NOT EXISTS idx_feedback_user_id ON feedback(user_id);
                    CREATE INDEX IF NOT EXISTS idx_feedback_tenant_id ON feedback(tenant_id);
                    CREATE INDEX IF NOT EXISTS idx_feedback_type ON feedback(feedback_type);
                    CREATE INDEX IF NOT EXISTS idx_feedback_status ON feedback(status);
                """)
                
                # You can add more tables here for extensibility
                # For example: user_preferences, query_history, etc.
                
                conn.commit()
    
    def purge_embeddings(self, db_id: Optional[int] = None, collection_name: str = "embedded_schema"):
        """
        Delete existing embeddings from the vector store.
        
        Args:
            db_id: Optional database ID to filter by
            collection_name: Name of the collection to purge
        """
        if db_id:
            print(f"Purging existing embeddings for db_id: {db_id}...")
        else:
            print("Purging all existing embeddings...")
        
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
                    print(f"Purged {deleted_count} existing embeddings")
        except Exception as e:
            print(f"Error purging embeddings: {e}")
            raise


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
                    
                    result_chat_id = str(cur.fetchone()[0])
                
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
                       feedback_type: str, message: str, user_agent: str = None, 
                       metadata: Dict = None) -> str:
        """Submit feedback for a chat"""
        with self.db.get_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("""
                    INSERT INTO feedback (chat_id, user_id, tenant_id, feedback_type, message, user_agent, metadata)
                    VALUES (%s, %s, %s, %s, %s, %s, %s)
                    RETURNING feedback_id
                """, (chat_id, user_id, tenant_id, feedback_type, message, user_agent, 
                      json.dumps(metadata) if metadata else None))
                
                feedback_id = str(cur.fetchone()[0])
                conn.commit()
                return feedback_id
    
    def get_feedback(self, feedback_id: str) -> Optional[Dict[str, Any]]:
        """Get a specific feedback entry"""
        with self.db.get_connection() as conn:
            with conn.cursor() as cur:
                cur.execute("""
                    SELECT f.feedback_id, f.chat_id, f.user_id, f.tenant_id, f.feedback_type,
                           f.message, f.user_agent, f.metadata, f.status, f.created_at, f.updated_at,
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
                    "chat_title": row[11]
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


# Global instances
db_manager = DatabaseManager()
chat_repository = ChatRepository(db_manager)
feedback_repository = FeedbackRepository(db_manager)