#!/bin/sh
set -e

echo "Starting Flask application (serving static + API) on port 8080..."
cat /app/frontend/browser/build-info.json

cd /app/backend/src

# Create the PGVector tables before Gunicorn starts workers that would race on first boot.
python -c 'from embeddings import embedding_manager'

exec gunicorn -w 2 --threads 2 -b 0.0.0.0:8080 --timeout 120 --no-control-socket --access-logfile - --error-logfile - app:app
