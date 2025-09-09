"""
Metabase API integration module.
Handles all interactions with Metabase including queries, cards, and embeddings.
"""
import requests
import jwt
import time
from typing import Dict, Any, List, Optional, Tuple
from config import config


class MetabaseClient:
    """Client for interacting with Metabase API"""
    
    def __init__(self):
        self.config = config.metabase
        self.headers = config.metabase_headers
    
    def execute_sql(self, sql: str, db_id: int) -> Dict[str, Any]:
        """
        Execute SQL query via Metabase API.
        
        Args:
            sql: SQL query to execute
            db_id: Database ID in Metabase
            
        Returns:
            Query results from Metabase
        """
        payload = {
            "database": db_id,
            "type": "native",
            "native": {"query": sql}
        }
        
        r = requests.post(
            f"{self.config.url}/api/dataset",
            headers=self.headers,
            json=payload
        )
        r.raise_for_status()
        return r.json()["data"]
    
    def validate_sql(self, sql: str, db_id: int) -> Tuple[bool, Optional[str]]:
        """
        Validate if SQL query can be executed.
        
        Args:
            sql: SQL query to validate
            db_id: Database ID
            
        Returns:
            Tuple of (is_valid, error_message)
        """
        payload = {
            "database": db_id,
            "type": "native",
            "native": {"query": sql}
        }
        
        r = requests.post(
            f"{self.config.url}/api/dataset",
            headers=self.headers,
            json=payload
        )
        
        if r.status_code not in (200, 202):
            return False, f"HTTP {r.status_code}: {r.text}"
        
        body = r.json()
        
        # Handle async queries
        if r.status_code == 202 and body.get("status") == "running":
            job_id = body["id"]
            deadline = time.time() + 10
            
            while time.time() < deadline:
                jr = requests.get(
                    f"{self.config.url}/api/async/{job_id}",
                    headers=self.headers
                )
                if jr.status_code == 200:
                    body = jr.json()
                    break
                time.sleep(0.5)
        
        if "error" in body:
            return False, body["error"]
        
        return True, None
    
    def get_database_metadata(self, db_id: int) -> Dict[str, Any]:
        """Get metadata for a database including tables and fields"""
        r = requests.get(
            f"{self.config.url}/api/database/{db_id}/metadata",
            headers=self.headers
        )
        r.raise_for_status()
        return r.json()
    
    def create_card(self, sql: str, db_id: int, collection_id: int, 
                    name: str) -> int:
        """
        Create a new Metabase card (saved question).
        
        Args:
            sql: SQL query for the card
            db_id: Database ID
            collection_id: Collection ID to save the card in
            name: Name of the card
            
        Returns:
            Card ID
        """
        url = f"{self.config.url}/api/card"
        payload = {
            "name": name,
            "visualization_settings": {},
            "collection_id": collection_id,
            "enable_embedding": True,
            "dataset_query": {
                "database": db_id,
                "native": {"query": sql},
                "type": "native"
            },
            "display": "table"
        }
        
        print(f"Metabase create_card - URL: {url}")
        print(f"Metabase create_card - Headers: {self.headers}")
        print(f"Metabase create_card - Payload keys: {list(payload.keys())}")
        print(f"Metabase create_card - SQL length: {len(sql)}")
        
        try:
            print("Making POST request to Metabase API...")
            r = requests.post(
                url,
                headers=self.headers,
                json=payload,
                timeout=30  # Add timeout
            )
            print(f"POST request completed - Status: {r.status_code}")
            
        except requests.exceptions.Timeout:
            print("Metabase request timed out after 30 seconds")
            raise Exception("Metabase API request timed out")
        except requests.exceptions.ConnectionError as e:
            print(f"Connection error to Metabase: {e}")
            raise Exception(f"Connection error to Metabase: {e}")
        except Exception as e:
            print(f"Unexpected error during Metabase request: {e}")
            raise Exception(f"Unexpected error during Metabase request: {e}")
        
        if r.status_code != 200:
            print(f"Metabase API error - Status: {r.status_code}, Response: {r.text}")
            raise Exception(f"HTTP {r.status_code}: {r.text}")
        
        try:
            response_json = r.json()
            card_id = response_json["id"]
            print(f"Card created successfully with ID: {card_id}")
        except Exception as e:
            print(f"Error parsing Metabase response: {e}")
            print(f"Response text: {r.text}")
            raise Exception(f"Error parsing Metabase response: {e}")
        
        # Enable embedding
        print(f"Enabling embedding for card {card_id}...")
        try:
            r2 = requests.put(
                f"{self.config.url}/api/card/{card_id}",
                headers=self.headers,
                json={"enable_embedding": True},
                timeout=30
            )
            print(f"Embedding enable request completed - Status: {r2.status_code}")
            
        except requests.exceptions.Timeout:
            print("Metabase embedding enable request timed out")
            raise Exception("Metabase embedding enable request timed out")
        except requests.exceptions.ConnectionError as e:
            print(f"Connection error enabling embedding: {e}")
            raise Exception(f"Connection error enabling embedding: {e}")
        
        if r2.status_code != 200:
            print(f"Error enabling embedding - Status: {r2.status_code}, Response: {r2.text}")
            raise Exception(f"HTTP {r2.status_code}: {r2.text}")
        
        print(f"Card {card_id} embedding enabled successfully")
        return card_id
    
    def update_card_visualization(self, card_id: int, display_mode: str,
                                 x_fields: List[str], y_fields: List[str]):
        """
        Update visualization settings for a card.
        
        Args:
            card_id: Card ID to update
            display_mode: Visualization type (bar, line, pie, map, etc.)
            x_fields: Fields for x-axis
            y_fields: Fields for y-axis
        """
        visualization_settings = {
            "graph.dimensions": x_fields,
            "graph.metrics": y_fields,
        }
        
        # Add specific settings for different chart types
        if display_mode == "pie":
            visualization_settings.update({
                "pie.dimension": x_fields,
                "pie.metric": y_fields[0] if y_fields else ""
            })
        elif display_mode == "map":
            visualization_settings["map.region"] = "1c5d50ee-4389-4593-37c1-fa8d4687ff4c"
        
        r = requests.put(
            f"{self.config.url}/api/card/{card_id}",
            headers=self.headers,
            json={
                "display": display_mode,
                "visualization_settings": visualization_settings
            }
        )
        
        if r.status_code != 200:
            raise Exception(f"HTTP {r.status_code}: {r.text}")
    
    def delete_card(self, card_id: int) -> bool:
        """Delete a Metabase card"""
        r = requests.delete(
            f"{self.config.url}/api/card/{card_id}",
            headers=self.headers
        )
        return r.status_code in (200, 204)
    
    def get_all_cards(self) -> List[int]:
        """Get all card IDs from Metabase"""
        try:
            r = requests.get(
                f"{self.config.url}/api/card",
                headers=self.headers
            )
            if r.status_code != 200:
                raise Exception(f"HTTP {r.status_code}: {r.text}")
            
            cards = r.json()
            return [card["id"] for card in cards]
        except Exception as e:
            print(f"Error getting cards from Metabase: {e}")
            return []
    
    def generate_embed_url(self, card_id: int) -> str:
        """Generate an embed URL for a card"""
        print(f"Generating embed URL for card {card_id}")
        
        try:
            payload = {
                "resource": {"question": card_id},
                "params": {}
            }
            print(f"JWT payload created: {payload}")
            
            if not self.config.embed_secret:
                raise Exception("Metabase embed_secret not configured")
            
            token = jwt.encode(
                payload,
                self.config.embed_secret,
                algorithm="HS256"
            )
            print(f"JWT token generated successfully (length: {len(token)})")
            
            embed_url = f"{self.config.url}/embed/question/{token}?bordered=true&titled=false"
            print(f"Embed URL generated: {embed_url[:100]}...")  # Truncate for logging
            
            return embed_url
            
        except Exception as e:
            print(f"Error generating embed URL: {e}")
            raise Exception(f"Error generating embed URL: {e}")
    
    def check_card_exists(self, card_id: int) -> bool:
        """Check if a card exists in Metabase"""
        existing_cards = self.get_all_cards()
        return card_id in existing_cards


# Global client instance
metabase_client = MetabaseClient()