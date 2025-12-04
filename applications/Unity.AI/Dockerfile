# Combined Frontend + Backend Docker Image
# Flask serves both the static Angular frontend AND the API endpoints
# No nginx needed - everything runs through Flask

# Stage 1: Build Angular Frontend
FROM node:22-alpine AS frontend-build

ARG UAI_BUILD_VERSION=0.0.0
ARG UAI_BUILD_REVISION=0000000

WORKDIR /app/frontend

# Copy frontend package files
COPY Unity.AI.Reporting.Frontend/package*.json ./

# Install frontend dependencies
RUN npm install

# Copy frontend source
COPY Unity.AI.Reporting.Frontend/ ./

# Build the Angular application
RUN npm run build -- --configuration=production

# Create build info file
RUN echo "{\"version\":\"${UAI_BUILD_VERSION}\",\"revision\":\"${UAI_BUILD_REVISION}\",\"buildDate\":\"$(date -u +%Y-%m-%dT%H:%M:%SZ)\"}" > dist/recap/build-info.json

# Stage 2: Runtime - Python/Flask only (serves static + API)
FROM python:3.11-slim

ARG UAI_BUILD_VERSION=0.0.0
ARG UAI_BUILD_REVISION=0000000

# Create a non-root user for OpenShift compatibility
RUN groupadd -g 1001 appuser && \
    useradd -r -u 1001 -g appuser -d /app -s /sbin/nologin appuser

# Create necessary directories
RUN mkdir -p /app/backend \
             /app/frontend \
             /tmp && \
    chmod -R 777 /tmp

# Copy built frontend from frontend-build stage
COPY --from=frontend-build /app/frontend/dist/recap /app/frontend

# Copy backend source code
COPY Unity.AI.Reporting.Backend/requirements.txt /app/backend/
COPY Unity.AI.Reporting.Backend/src/ /app/backend/src/

# Install Python dependencies
WORKDIR /app/backend
RUN pip install --no-cache-dir -r requirements.txt gunicorn

# Create entrypoint script that generates config.json and starts Flask
RUN cat > /entrypoint.sh <<'EOF'
#!/bin/sh
set -e

# Default values
ENVIRONMENT="${ENVIRONMENT:-production}"
VERSION="${VERSION:-unknown}"

echo "Generating runtime configuration..."
echo "  ENVIRONMENT: ${ENVIRONMENT}"
echo "  VERSION: ${VERSION}"

# Generate config.json - Angular will call backend on same origin
# Angular 17+ puts files in browser subdirectory
if [ -d "/app/frontend/browser" ]; then
  CONFIG_PATH="/app/frontend/browser/config.json"
else
  CONFIG_PATH="/app/frontend/config.json"
fi

cat > "${CONFIG_PATH}" <<CONFIGEOF
{
  "environment": "${ENVIRONMENT}",
  "version": "${VERSION}"
}
CONFIGEOF

echo "Configuration generated successfully at ${CONFIG_PATH}"
echo "  Angular app will call: /api (same origin - Flask serves everything)"

# Start Flask with gunicorn on port 8080 (non-privileged port)
echo "Starting Flask application (serving static + API) on port 8080..."
cd /app/backend/src
exec gunicorn -w 4 -b 0.0.0.0:8080 --access-logfile - --error-logfile - app:app
EOF

# Make entrypoint executable (777 for OpenShift compatibility)
RUN chmod 777 /entrypoint.sh && \
    chown appuser:appuser /entrypoint.sh

# Change ownership
RUN chown -R appuser:appuser /app

# Expose port 8080
EXPOSE 8080

# Run as non-root user
USER 1001

# Use entrypoint
ENTRYPOINT ["/entrypoint.sh"]
