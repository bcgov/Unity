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

# Expose port 80
EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]