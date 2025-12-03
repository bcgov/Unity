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

# Use OpenShift compatible nginx image for serving the application
FROM nginx:alpine

# Create a non-root user for OpenShift compatibility
RUN addgroup -g 1001 -S nginx-openshift && \
    adduser -S -D -H -u 1001 -h /var/cache/nginx -s /sbin/nologin -G nginx-openshift -g nginx-openshift nginx-openshift

# Create writable directories for nginx temporary files (OpenShift requirement)
RUN mkdir -p /var/cache/nginx/client_temp \
             /var/cache/nginx/proxy_temp \
             /var/cache/nginx/fastcgi_temp \
             /var/cache/nginx/uwsgi_temp \
             /var/cache/nginx/scgi_temp && \
    chmod -R 777 /var/cache/nginx && \
    chmod -R 777 /var/run && \
    chmod -R 777 /var/log/nginx

# Copy built application from build stage (includes build-info.json)
COPY --from=build /app/dist/recap /usr/share/nginx/html

# Copy nginx configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy entrypoint script for runtime configuration
COPY entrypoint.sh /entrypoint.sh

# Fix line endings (Windows CRLF -> Unix LF) and make executable
RUN sed -i 's/\r$//' /entrypoint.sh && chmod +x /entrypoint.sh

# Update nginx configuration for OpenShift compatibility
RUN sed -i '/^user/d' /etc/nginx/nginx.conf && \
    sed -i '/pid.*nginx.pid/d' /etc/nginx/nginx.conf && \
    echo 'pid /tmp/nginx.pid;' >> /etc/nginx/nginx.conf

# Change ownership of necessary directories to non-root user
RUN chown -R nginx-openshift:nginx-openshift /usr/share/nginx/html \
                                            /var/cache/nginx \
                                            /etc/nginx/conf.d && \
    chmod -R 755 /usr/share/nginx/html

# Expose port 8080 (non-privileged port for OpenShift)
EXPOSE 8080

# Run as non-root user
USER 1001

# Use entrypoint script to generate config.json at runtime and start nginx
ENTRYPOINT ["/entrypoint.sh"]
