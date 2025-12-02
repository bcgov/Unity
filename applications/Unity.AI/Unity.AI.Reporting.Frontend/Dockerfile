# Multi-stage build for Angular application
FROM node:22-alpine AS build

# Platform build metadata (safe to bake into image)
ARG UAI_BUILD_VERSION=0.0.0
ARG UAI_BUILD_REVISION=0000000

WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm install

# Copy source code
COPY . .

# Build the application for Docker first
RUN npm run build -- --configuration=production

# Create build info file with version and revision in the root of the built output
RUN echo "{\"version\":\"${UAI_BUILD_VERSION}\",\"revision\":\"${UAI_BUILD_REVISION}\",\"buildDate\":\"$(date -u +%Y-%m-%dT%H:%M:%SZ)\"}" > dist/recap/build-info.json

# Production stage
FROM nginx:alpine

# Copy built application from build stage (includes build-info.json)
COPY --from=build /app/dist/recap /usr/share/nginx/html

# Copy nginx configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy entrypoint script for runtime configuration
COPY docker-entrypoint.sh /docker-entrypoint.sh
# Fix line endings (Windows CRLF -> Unix LF) and make executable
RUN sed -i 's/\r$//' /docker-entrypoint.sh && chmod +x /docker-entrypoint.sh

# Create nginx cache directories and set permissions
RUN mkdir -p /var/cache/nginx/client_temp \
    /var/cache/nginx/proxy_temp \
    /var/cache/nginx/fastcgi_temp \
    /var/cache/nginx/uwsgi_temp \
    /var/cache/nginx/scgi_temp && \
    chmod -R 755 /var/cache/nginx && \
    chown -R nginx:nginx /var/cache/nginx && \
    chmod -R 755 /usr/share/nginx/html

# Expose port 80
EXPOSE 80

# Use entrypoint script to generate config.json at runtime and start nginx
ENTRYPOINT ["/docker-entrypoint.sh"]