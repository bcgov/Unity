# Multi-stage build for Angular application
FROM node:22-alpine as build

WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm install

# Copy source code
COPY . .

# Build the application for Docker
RUN npm run build -- --configuration=production

# Production stage
FROM nginx:alpine

# Copy built application from build stage
COPY --from=build /app/dist/recap /usr/share/nginx/html

# Copy nginx configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy config template to the html directory
COPY config.template.json /usr/share/nginx/html/browser/config.json

# Create entrypoint script for runtime config replacement
RUN echo '#!/bin/sh' > /docker-entrypoint.sh && \
    echo 'set -e' >> /docker-entrypoint.sh && \
    echo '' >> /docker-entrypoint.sh && \
    echo '# Replace placeholders in config.json with environment variables' >> /docker-entrypoint.sh && \
    echo 'CONFIG_FILE="/usr/share/nginx/html/browser/config.json"' >> /docker-entrypoint.sh && \
    echo '' >> /docker-entrypoint.sh && \
    echo '# Set defaults if not provided' >> /docker-entrypoint.sh && \
    echo 'API_URL="${API_URL:-http://localhost:5000/api}"' >> /docker-entrypoint.sh && \
    echo 'ENVIRONMENT="${ENVIRONMENT:-production}"' >> /docker-entrypoint.sh && \
    echo 'VERSION="${VERSION:-1.0.0}"' >> /docker-entrypoint.sh && \
    echo '' >> /docker-entrypoint.sh && \
    echo 'echo "Configuring application with:"' >> /docker-entrypoint.sh && \
    echo 'echo "  API_URL: $API_URL"' >> /docker-entrypoint.sh && \
    echo 'echo "  ENVIRONMENT: $ENVIRONMENT"' >> /docker-entrypoint.sh && \
    echo 'echo "  VERSION: $VERSION"' >> /docker-entrypoint.sh && \
    echo '' >> /docker-entrypoint.sh && \
    echo '# Replace placeholders in config.json' >> /docker-entrypoint.sh && \
    echo 'sed -i "s|__API_URL_PLACEHOLDER__|${API_URL}|g" "$CONFIG_FILE"' >> /docker-entrypoint.sh && \
    echo 'sed -i "s|__ENVIRONMENT_PLACEHOLDER__|${ENVIRONMENT}|g" "$CONFIG_FILE"' >> /docker-entrypoint.sh && \
    echo 'sed -i "s|__VERSION_PLACEHOLDER__|${VERSION}|g" "$CONFIG_FILE"' >> /docker-entrypoint.sh && \
    echo '' >> /docker-entrypoint.sh && \
    echo 'echo "Configuration complete. Starting nginx..."' >> /docker-entrypoint.sh && \
    echo '' >> /docker-entrypoint.sh && \
    echo '# Start nginx' >> /docker-entrypoint.sh && \
    echo 'exec nginx -g "daemon off;"' >> /docker-entrypoint.sh && \
    chmod +x /docker-entrypoint.sh

# Expose port 80
EXPOSE 80

ENTRYPOINT ["/docker-entrypoint.sh"]