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

# Copy built application from build stage
COPY --from=build /app/dist/recap /usr/share/nginx/html

# Copy nginx configuration
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Expose port 80
EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]