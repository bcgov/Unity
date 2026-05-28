#!/bin/sh
set -e

echo "Starting Flask application (serving static + API) on port 8080..."

cd /app/backend/src
exec gunicorn --preload -w 2 --threads 2 -b 0.0.0.0:8080 --timeout 120 --access-logfile - --error-logfile - app:app
