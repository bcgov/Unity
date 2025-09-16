import os
import requests

metabase_url = "https://test-unity-reporting.apps.silver.devops.gov.bc.ca"
headers = {"x-api-key": os.getenv("METABASE_KEY")}

def list_card_creators():
    """List who created and updated each card in Metabase"""
    try:
        # Get all cards/questions from Metabase
        response = requests.get(f"{metabase_url}/api/card", headers=headers)
        
        if response.status_code != 200:
            print(f"Error fetching cards: HTTP {response.status_code} - {response.text}")
            return
        
        cards = response.json()
        
        print(f"\nMetabase Cards - Created/Updated By Report")
        print(f"Total cards: {len(cards)}")
        print(f"{'='*80}\n")
        
        for card in cards:
            print(f"Card ID: {card.get('id')} - {card.get('name', 'Untitled')}")
            print(f"  Created by: User ID {card.get('creator_id')}")
            print(f"  Created at: {card.get('created_at')}")
            print(f"  Last edited by: User ID {card.get('last-edit-info', {}).get('id', 'Unknown')}")
            print(f"  Updated at: {card.get('updated_at')}")
            print()
        
    except Exception as e:
        print(f"Error listing cards: {e}")

if __name__ == "__main__":
    list_card_creators()