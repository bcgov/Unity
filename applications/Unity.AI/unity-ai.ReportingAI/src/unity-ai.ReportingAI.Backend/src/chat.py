"""
Chat management module.
Handles conversation state and Metabase card recreation.
"""
from typing import List, Dict, Any, Optional
from database import chat_repository
from metabase import metabase_client
from config import config


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
        metabase_url = chat_data["metabase_url"]
        tenant_id = chat_data["tenant_id"]
        
        # Get tenant configuration
        tenant_config = config.get_tenant_config(tenant_id)
        db_id = tenant_config["db_id"]
        collection_id = tenant_config["collection_id"]
        
        # Check and recreate cards if needed
        updated_conversation = self._validate_and_recreate_cards(
            conversation, metabase_url, db_id, collection_id
        )
        
        # Update database if cards were recreated
        if conversation != updated_conversation:
            self.repository.update_chat_cards(chat_id, user_id, updated_conversation)
        
        return {"conversation": updated_conversation}
    
    def _validate_and_recreate_cards(self, conversation: List[Dict], 
                                    metabase_url: str, db_id: int, 
                                    collection_id: int) -> List[Dict]:
        """
        Validate and recreate missing Metabase cards in conversation.
        
        Args:
            conversation: Chat conversation with card references
            metabase_url: Metabase URL
            db_id: Database ID
            collection_id: Collection ID
            
        Returns:
            Updated conversation with recreated cards
        """
        # Get all existing card IDs from Metabase
        existing_card_ids = self.metabase.get_all_cards()
        updated_conversation = []
        
        for turn in conversation:
            if 'embed' in turn and turn['embed'] and 'card_id' in turn['embed']:
                card_id = turn['embed']['card_id']
                
                # If card doesn't exist, recreate it
                if card_id not in existing_card_ids:
                    embed_data = turn['embed']
                    sql = embed_data.get('SQL', '')
                    title = embed_data.get('title', 'Untitled')
                    
                    if sql:
                        try:
                            # Create new card
                            new_card_id = self.metabase.create_card(
                                sql, db_id, collection_id, title
                            )
                            
                            # Apply visualization settings if available
                            viz_type = embed_data.get('current_visualization')
                            x_fields = embed_data.get('x_field', [])
                            y_fields = embed_data.get('y_field', [])
                            
                            if viz_type and x_fields and y_fields:
                                try:
                                    self.metabase.update_card_visualization(
                                        new_card_id, viz_type, x_fields, y_fields
                                    )
                                except Exception as e:
                                    print(f"Error updating visualization: {e}")
                            
                            # Generate new embed URL
                            new_embed_url = self.metabase.generate_embed_url(new_card_id)
                            
                            # Update turn with new card info
                            turn['embed']['card_id'] = new_card_id
                            turn['embed']['url'] = new_embed_url
                            
                            print(f"Recreated card {card_id} as {new_card_id}")
                        except Exception as e:
                            print(f"Error recreating card {card_id}: {e}")
            
            updated_conversation.append(turn)
        
        return updated_conversation
    
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