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

# Create build-info.json 
RUN echo "{\"version\":\"${UAI_BUILD_VERSION}\",\"revision\":\"${UAI_BUILD_REVISION}\",\"buildDate\":\"$(date -u +%Y-%m-%dT%H:%M:%SZ)\",\"environment\":\"Production\"}" > dist/recap/browser/build-info.json

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

# Create simple entrypoint script that starts Flask
RUN echo '#!/bin/sh' > /entrypoint.sh && \
echo 'echo "Starting Flask application (serving static + API) on port 8080..."' >> /entrypoint.sh && \
echo 'cd /app/backend/src' >> /entrypoint.sh && \
echo 'exec gunicorn --preload -w 2 --threads 2 -b 0.0.0.0:8080 --timeout 120 --access-logfile - --error-logfile - app:app' >> /entrypoint.sh

# Make entrypoint executable (777 for OpenShift compatibility)
RUN chmod 777 /entrypoint.sh && \
    chown appuser:appuser /entrypoint.sh

# Change ownership
RUN chown -R appuser:appuser /app

# Expose port 8080
EXPOSE 8080

# Run as non-root user
USER 1001

# Use entrypoint (exec form with shell)
ENTRYPOINT ["/bin/sh", "/entrypoint.sh"]
