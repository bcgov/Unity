"""
Chat management module.
Handles conversation state and Metabase card recreation.
"""
import logging
from typing import List, Dict, Any, Optional
from database import chat_repository
from metabase import metabase_client
from config import config

# Configure logging
logger = logging.getLogger(__name__)


class ChatManager:
    """Manages chat conversations and their associated Metabase cards"""
    
    def __init__(self):
        self.repository = chat_repository
        self.metabase = metabase_client
    
    def get_user_chats(self, user_id: str, tenant_id: str) -> List[Dict[str, Any]]:
        """Get all chats for a user"""
        return self.repository.get_user_chats(user_id, tenant_id)
    
    def get_chat_with_card_validation(self, chat_id: str, user_id: str) -> Dict[str, Any]:
        """
        Get a chat and recreate any missing Metabase cards.
        
        Args:
            chat_id: Chat ID
            user_id: User ID
            
        Returns:
            Chat data with validated/recreated cards
        """
        chat_data = self.repository.get_chat(chat_id, user_id)
        if not chat_data:
            return None
        
        conversation = chat_data["conversation"]
        tenant_id = chat_data["tenant_id"]
        
        # Get tenant configuration
        tenant_config = config.get_tenant_config(tenant_id)
        db_id = tenant_config["db_id"]
        collection_id = tenant_config["collection_id"]
        
        # Check and recreate cards if needed
        updated_conversation = self._validate_and_recreate_cards(
            conversation, db_id, collection_id, tenant_id=tenant_id
        )
        
        # Update database if cards were recreated
        if conversation != updated_conversation:
            self.repository.update_chat_cards(chat_id, user_id, updated_conversation)
        
        return {"conversation": updated_conversation}
    
    def _validate_and_recreate_cards(self, conversation: List[Dict],
                                    db_id: int,
                                    collection_id: int,
                                    tenant_id: Optional[str] = None) -> List[Dict]:
        """
        Validate and recreate missing Metabase cards in conversation.

        Args:
            conversation: Chat conversation with card references
            db_id: Database ID
            collection_id: Collection ID
            tenant_id: Optional tenant ID for tenant-specific Metabase API key

        Returns:
            Updated conversation with recreated cards
        """
        # Get all existing card IDs from Metabase
        existing_card_ids = self.metabase.get_all_cards(tenant_id=tenant_id)

        for turn in conversation:
            self._recreate_card_if_missing(turn, existing_card_ids, db_id, collection_id,
                                           tenant_id=tenant_id)

        return conversation

    def _recreate_card_if_missing(self, turn: Dict, existing_card_ids: List,
                                  db_id: int, collection_id: int,
                                  tenant_id: Optional[str] = None) -> None:
        """Recreate a Metabase card for a turn if it no longer exists."""
        embed_data = turn.get('embed')
        if not embed_data or 'card_id' not in embed_data:
            return

        card_id = embed_data['card_id']

        # Card still exists — nothing to do
        if card_id in existing_card_ids:
            return

        sql = embed_data.get('SQL', '')
        if not sql:
            return

        title = embed_data.get('title', 'Untitled')
        try:
            # Create new card
            new_card_id = self.metabase.create_card(
                sql, db_id, collection_id, title,
                tenant_id=tenant_id
            )

            # Apply visualization settings if available
            self._apply_visualization(new_card_id, embed_data, tenant_id=tenant_id)

            # Generate new embed URL
            new_embed_url = self.metabase.generate_embed_url(new_card_id)

            # Update turn with new card info
            embed_data['card_id'] = new_card_id
            embed_data['url'] = new_embed_url

            logger.info(f"Recreated card {card_id} as {new_card_id}")
        except Exception as e:
            logger.error(f"Error recreating card {card_id}: {e}", exc_info=True)

    def _apply_visualization(self, card_id: int, embed_data: Dict,
                             tenant_id: Optional[str] = None) -> None:
        """Apply visualization settings to a card if all fields are present."""
        viz_type = embed_data.get('current_visualization')
        x_fields = embed_data.get('x_field', [])
        y_fields = embed_data.get('y_field', [])

        if not (viz_type and x_fields and y_fields):
            return

        try:
            self.metabase.update_card_visualization(
                card_id, viz_type, x_fields, y_fields,
                tenant_id=tenant_id
            )
        except Exception as e:
            logger.error(f"Error updating visualization: {e}", exc_info=True)
    
    def save_chat(self, user_id: str, tenant_id: str, metabase_url: str,
                  title: str, conversation: List[Dict], 
                  chat_id: Optional[str] = None) -> str:
        """Save or update a chat"""
        return self.repository.save_chat(
            user_id, tenant_id, metabase_url, title, conversation, chat_id
        )
    
    def delete_chat(self, chat_id: str, user_id: str) -> bool:
        """Delete a chat"""
        return self.repository.delete_chat(chat_id, user_id)
    
    def extract_past_questions(self, conversation: List[Dict]) -> List[Dict]:
        """
        Extract past questions and SQL from conversation history.
        
        Args:
            conversation: Chat conversation history
            
        Returns:
            List of past questions with their SQL
        """
        past_questions = []
        
        for turn in conversation:
            if 'question' in turn and 'embed' in turn and 'SQL' in turn['embed']:
                past_questions.append({
                    "question": turn["question"],
                    "SQL": turn["embed"]["SQL"]
                })
        
        return past_questions


# Global chat manager instance
chat_manager = ChatManager()