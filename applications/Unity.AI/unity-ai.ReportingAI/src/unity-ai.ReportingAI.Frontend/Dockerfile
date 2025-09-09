# Multi-stage build for Angular application
FROM node:22-alpine as build

# Accept build arguments
ARG JWT_SECRET

WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm install

# Copy source code
COPY . .

# Replace JWT_SECRET placeholder in environment files
RUN sed -i "s/__JWT_SECRET_PLACEHOLDER__/${JWT_SECRET}/g" src/environments/environment.prod.ts

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