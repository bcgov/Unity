import logging
import requests
from config import config

# Configure logging
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(name)s - %(levelname)s - %(message)s'
)
logger = logging.getLogger(__name__)

metabase_url = config.metabase.url
headers = config.metabase_headers

def list_card_creators():
    """List who created and updated each card in Metabase"""
    try:
        # Get all cards/questions from Metabase
        response = requests.get(f"{metabase_url}/api/card", headers=headers)

        if response.status_code != 200:
            logger.error(f"Error fetching cards: HTTP {response.status_code} - {response.text}")
            return

        cards = response.json()

        logger.info("\nMetabase Cards - Created/Updated By Report")
        logger.info(f"Total cards: {len(cards)}")
        logger.info(f"{'='*80}\n")

        for card in cards:
            logger.info(f"Card ID: {card.get('id')} - {card.get('name', 'Untitled')}")
            logger.info(f"  Created by: User ID {card.get('creator_id')}")
            logger.info(f"  Created at: {card.get('created_at')}")
            logger.info(f"  Last edited by: User ID {card.get('last-edit-info', {}).get('id', 'Unknown')}")
            logger.info(f"  Updated at: {card.get('updated_at')}")
            logger.info("")

    except Exception as e:
        logger.exception(f"Error listing cards: {e}")

if __name__ == "__main__":
    list_card_creators()