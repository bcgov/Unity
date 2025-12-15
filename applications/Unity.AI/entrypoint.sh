#!/bin/sh
set -e

echo "Generating tenant_config.json from environment variables..."

# Generate tenant_config.json using Python (use 'python' not 'python3' in python:3.11-slim)
python -c "
import json
import os

config = {
    'Cyrus Org': {
        'db_id': 3,
        'collection_id': 47,
        'schema_types': ['public'],
        'api_key': os.getenv('METABASE_KEY', '')
    },
    'default': {
        'db_id': int(os.getenv('DEFAULT_EMBED_DB_ID', '5')),
        'collection_id': 19,
        'schema_types': ['public'],
        'api_key': os.getenv('METABASE_KEY', '')
    }
}

with open('/app/backend/src/tenant_config.json', 'w') as f:
    json.dump(config, f, indent=2)
"

echo "tenant_config.json created successfully"
cat /app/backend/src/tenant_config.json
echo ""
echo "Starting Flask application (serving static + API) on port 8080..."

cd /app/backend/src
exec gunicorn --preload -w 2 --threads 2 -b 0.0.0.0:8080 --timeout 120 --access-logfile - --error-logfile - app:app
