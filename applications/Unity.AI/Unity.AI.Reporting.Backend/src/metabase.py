"""
Metabase API integration module.
Handles all interactions with Metabase including queries, cards, and embeddings.
"""
import requests
import jwt
import time
import logging
from typing import Dict, Any, List, Optional, Tuple
from config import config

# Configure logging
logger = logging.getLogger(__name__)


class MetabaseClient:
    """Client for interacting with Metabase API"""

    def __init__(self):
        self.config = config.metabase
        self.headers = config.metabase_headers

    def _get_headers(self, tenant_id: Optional[str] = None) -> Dict[str, str]:
        """
        Get API headers for Metabase requests.
        If tenant_id is provided, uses tenant-specific API key from config file.
        Otherwise uses default headers.
        """
        if tenant_id:
            return config.get_tenant_metabase_headers(tenant_id)
        return self.headers

    def execute_sql(self, sql: str, db_id: int, tenant_id: Optional[str] = None) -> Dict[str, Any]:
        """
        Execute SQL query via Metabase API.

        Args:
            sql: SQL query to execute
            db_id: Database ID in Metabase
            tenant_id: Optional tenant ID to use tenant-specific API key

        Returns:
            Query results from Metabase
        """
        payload = {
            "database": db_id,
            "type": "native",
            "native": {"query": sql}
        }

        headers = self._get_headers(tenant_id)

        r = requests.post(
            f"{self.config.url}/api/dataset",
            headers=headers,
            json=payload
        )
        r.raise_for_status()
        return r.json()["data"]
    
    def validate_sql(self, sql: str, db_id: int, tenant_id: Optional[str] = None) -> Tuple[bool, Optional[str]]:
        """
        Validate if SQL query can be executed.

        Args:
            sql: SQL query to validate
            db_id: Database ID
            tenant_id: Optional tenant ID to use tenant-specific API key

        Returns:
            Tuple of (is_valid, error_message)
        """
        payload = {
            "database": db_id,
            "type": "native",
            "native": {"query": sql}
        }

        headers = self._get_headers(tenant_id)

        r = requests.post(
            f"{self.config.url}/api/dataset",
            headers=headers,
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
                    headers=headers
                )
                if jr.status_code == 200:
                    body = jr.json()
                    break
                time.sleep(0.5)

        if "error" in body:
            return False, body["error"]

        return True, None
    
    def get_database_metadata(self, db_id: int, tenant_id: Optional[str] = None) -> Dict[str, Any]:
        """Get metadata for a database including tables and fields"""
        headers = self._get_headers(tenant_id)
        r = requests.get(
            f"{self.config.url}/api/database/{db_id}/metadata",
            headers=headers
        )
        r.raise_for_status()
        return r.json()

    def create_card(self, sql: str, db_id: int, collection_id: int,
                    name: str, tenant_id: Optional[str] = None) -> int:
        """
        Create a new Metabase card (saved question).

        Args:
            sql: SQL query for the card
            db_id: Database ID
            collection_id: Collection ID to save the card in
            name: Name of the card
            tenant_id: Optional tenant ID to use tenant-specific API key

        Returns:
            Card ID
        """
        headers = self._get_headers(tenant_id)
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

        logger.debug(f"Metabase create_card - URL: {url}")
        logger.debug(f"Metabase create_card - Headers: {headers}")
        logger.debug(f"Metabase create_card - Payload keys: {list(payload.keys())}")
        logger.debug(f"Metabase create_card - SQL length: {len(sql)}")

        try:
            logger.info("Making POST request to Metabase API...")
            r = requests.post(
                url,
                headers=headers,
                json=payload,
                timeout=30  # Add timeout
            )
            logger.info(f"POST request completed - Status: {r.status_code}")

        except requests.exceptions.Timeout:
            logger.error("Metabase request timed out after 30 seconds")
            raise requests.exceptions.Timeout("Metabase API request timed out")
        except requests.exceptions.ConnectionError as e:
            logger.error(f"Connection error to Metabase: {e}", exc_info=True)
            raise requests.exceptions.ConnectionError(f"Connection error to Metabase: {e}")
        except Exception as e:
            logger.error(f"Unexpected error during Metabase request: {e}", exc_info=True)
            raise requests.exceptions.RequestException(f"Unexpected error during Metabase request: {e}")

        if r.status_code != 200:
            logger.error(f"Metabase API error - Status: {r.status_code}, Response: {r.text}")
            raise Exception(f"HTTP {r.status_code}: {r.text}")

        try:
            response_json = r.json()
            card_id = response_json["id"]
            logger.info(f"Card created successfully with ID: {card_id}")
        except Exception as e:
            logger.error(f"Error parsing Metabase response: {e}", exc_info=True)
            logger.error(f"Response text: {r.text}")
            raise ValueError(f"Error parsing Metabase response: {e}")

        # Enable embedding
        logger.info(f"Enabling embedding for card {card_id}...")
        try:
            r2 = requests.put(
                f"{self.config.url}/api/card/{card_id}",
                headers=headers,
                json={"enable_embedding": True},
                timeout=30
            )
            logger.info(f"Embedding enable request completed - Status: {r2.status_code}")

        except requests.exceptions.Timeout:
            logger.error("Metabase embedding enable request timed out")
            raise requests.exceptions.Timeout("Metabase embedding enable request timed out")
        except requests.exceptions.ConnectionError as e:
            logger.error(f"Connection error enabling embedding: {e}", exc_info=True)
            raise requests.exceptions.ConnectionError(f"Connection error enabling embedding: {e}")

        if r2.status_code != 200:
            logger.error(f"Error enabling embedding - Status: {r2.status_code}, Response: {r2.text}")
            raise Exception(f"HTTP {r2.status_code}: {r2.text}")

        logger.info(f"Card {card_id} embedding enabled successfully")
        return card_id
    
    def update_card_visualization(self, card_id: int, display_mode: str,
                                 x_fields: List[str], y_fields: List[str],
                                 tenant_id: Optional[str] = None):
        """
        Update visualization settings for a card.

        Args:
            card_id: Card ID to update
            display_mode: Visualization type (bar, line, pie, map, etc.)
            x_fields: Fields for x-axis
            y_fields: Fields for y-axis
            tenant_id: Optional tenant ID to use tenant-specific API key
        """
        headers = self._get_headers(tenant_id)
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
            headers=headers,
            json={
                "display": display_mode,
                "visualization_settings": visualization_settings
            }
        )

        if r.status_code != 200:
            raise Exception(f"HTTP {r.status_code}: {r.text}")

    def delete_card(self, card_id: int, tenant_id: Optional[str] = None) -> bool:
        """Delete a Metabase card"""
        headers = self._get_headers(tenant_id)
        r = requests.delete(
            f"{self.config.url}/api/card/{card_id}",
            headers=headers
        )
        return r.status_code in (200, 204)

    def get_all_cards(self, tenant_id: Optional[str] = None) -> List[int]:
        """Get all card IDs from Metabase"""
        headers = self._get_headers(tenant_id)
        try:
            r = requests.get(
                f"{self.config.url}/api/card",
                headers=headers
            )
            if r.status_code != 200:
                raise Exception(f"HTTP {r.status_code}: {r.text}")

            cards = r.json()
            return [card["id"] for card in cards]
        except Exception as e:
            logger.error(f"Error getting cards from Metabase: {e}", exc_info=True)
            return []
    
    def generate_embed_url(self, card_id: int) -> str:
        """Generate an embed URL for a card"""
        logger.info(f"Generating embed URL for card {card_id}")

        try:
            payload = {
                "resource": {"question": card_id},
                "params": {}
            }
            logger.debug(f"JWT payload created: {payload}")

            if not self.config.embed_secret:
                raise ValueError("Metabase embed_secret not configured")

            token = jwt.encode(
                payload,
                self.config.embed_secret,
                algorithm="HS256"
            )
            logger.debug(f"JWT token generated successfully (length: {len(token)})")

            embed_url = f"{self.config.url}/embed/question/{token}?bordered=true&titled=false"
            logger.info(f"Embed URL generated: {embed_url[:100]}...")

            return embed_url

        except Exception as e:
            logger.error(f"Error generating embed URL: {e}", exc_info=True)
            raise requests.exceptions.ConnectionError(f"Error generating embed URL: {e}")
    
    def check_card_exists(self, card_id: int, tenant_id: Optional[str] = None) -> bool:
        """Check if a card exists in Metabase"""
        existing_cards = self.get_all_cards(tenant_id)
        return card_id in existing_cards


# Global client instance
metabase_client = MetabaseClient()