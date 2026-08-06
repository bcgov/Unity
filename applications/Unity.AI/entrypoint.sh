#!/bin/sh
set -e

echo "Starting Flask application (serving static + API) on port 8080..."
cat /app/frontend/browser/build-info.json

cd /app/backend/src
# No --preload: app.py's startup schema-embed runs in a background thread per
# worker (see app.py). With --preload that thread would start in the master
# before workers are forked, which is unsafe (forking a process with a live
# background thread holding open DB/HTTP connections can leave workers with
# corrupted connection state). Without --preload each worker imports and
# backgrounds the embed independently after its own fork, so no more forking
# happens while it runs.
exec gunicorn -w 2 --threads 2 -b 0.0.0.0:8080 --timeout 120 --no-control-socket --access-logfile - --error-logfile - app:app
