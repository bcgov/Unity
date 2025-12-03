#!/bin/sh
set -e

# Generate config.json from environment variables at runtime
# This allows the container to be reconfigured without rebuilding the image

# Default values if not provided
BACKEND_API_URL="${API_URL:-http://reporting-backend:5000/api}"
ENVIRONMENT="${ENVIRONMENT:-Development}"
VERSION="${VERSION:-unknown}"

echo "Generating runtime configuration..."
echo "  Backend API_URL: ${BACKEND_API_URL}"
echo "  ENVIRONMENT: ${ENVIRONMENT}"
echo "  VERSION: ${VERSION}"

# Generate config.json - always use /api (relative path through nginx)
cat > /usr/share/nginx/html/browser/config.json <<EOF
{
  "apiUrl": "/api",
  "environment": "${ENVIRONMENT}",
  "version": "${VERSION}"
}
EOF

echo "Configuration generated successfully at /usr/share/nginx/html/browser/config.json"
echo "  Angular app will use: /api (proxied by nginx)"

# Extract backend URL from API_URL for nginx proxy
# Remove /api suffix to get the base backend URL
BACKEND_BASE_URL=$(echo "$BACKEND_API_URL" | sed 's|/api$||')

echo "Configuring nginx proxy..."
echo "  Nginx will proxy /api/* to: ${BACKEND_BASE_URL}"

# Update nginx config with the backend URL (keep the http:// prefix)
sed -i "s|proxy_pass http://reporting-backend:5000;|proxy_pass ${BACKEND_BASE_URL};|g" /etc/nginx/conf.d/default.conf

echo "Nginx configuration updated successfully"

# Start nginx
echo "Starting nginx..."
exec nginx -g "daemon off;"
