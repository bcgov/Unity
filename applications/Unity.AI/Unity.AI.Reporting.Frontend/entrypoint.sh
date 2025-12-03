#!/bin/sh
set -e

# Generate config.json from environment variables at runtime
# This allows the container to be reconfigured without rebuilding the image

# Default values if not provided
API_URL="${API_URL:-/api}"
ENVIRONMENT="${ENVIRONMENT:-Development}"
VERSION="${VERSION:-unknown}"

echo "Generating runtime configuration..."
echo "  API_URL: ${API_URL}"
echo "  ENVIRONMENT: ${ENVIRONMENT}"
echo "  VERSION: ${VERSION}"

# Generate config.json
cat > /usr/share/nginx/html/browser/config.json <<EOF
{
  "apiUrl": "${API_URL}",
  "environment": "${ENVIRONMENT}",
  "version": "${VERSION}"
}
EOF

echo "Configuration generated successfully at /usr/share/nginx/html/browser/config.json"

# Start nginx
echo "Starting nginx..."
exec nginx -g "daemon off;"
