# Multi-stage build for Angular application
FROM node:22-alpine AS build

WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm install

# Copy source code
COPY . .

# Build the application
RUN npm run build

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

# Copy built application from build stage
COPY --from=build /app/dist /usr/share/nginx/html

# Create custom nginx configuration for OpenShift compatibility 
RUN cat > /etc/nginx/conf.d/default.conf <<EOF
server {
    listen 8080;
    server_name localhost;
    root /usr/share/nginx/html;
    index index.html;

    # OpenShift-friendly temp directories
    client_body_temp_path /var/cache/nginx/client_temp;
    proxy_temp_path /var/cache/nginx/proxy_temp;
    fastcgi_temp_path /var/cache/nginx/fastcgi_temp;
    uwsgi_temp_path /var/cache/nginx/uwsgi_temp;
    scgi_temp_path /var/cache/nginx/scgi_temp;

    location / {
        try_files \$uri \$uri/ /index.html;
    }

    # Security headers  
    add_header X-Content-Type-Options "nosniff" always;
    add_header X-XSS-Protection "1; mode=block" always;
}
EOF

# Update main nginx.conf for non-root operation
RUN sed -i '/^user/d' /etc/nginx/nginx.conf && \
    sed -i '/pid.*nginx.pid/d' /etc/nginx/nginx.conf && \
    echo 'pid /tmp/nginx.pid;' >> /etc/nginx/nginx.conf

# Change ownership of necessary directories
RUN chown -R nginx-openshift:nginx-openshift /usr/share/nginx/html \
                                            /var/cache/nginx \
                                            /etc/nginx/conf.d

# Expose port 8080 (non-privileged port for OpenShift compatibility)
EXPOSE 8080

# Run as non-root user
USER 1001

CMD ["nginx", "-g", "daemon off;"]