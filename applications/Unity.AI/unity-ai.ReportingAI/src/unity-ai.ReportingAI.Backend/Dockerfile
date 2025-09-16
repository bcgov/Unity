FROM python:3.11-slim

# Accept build arguments
ARG AZURE_OPENAI_ENDPOINT
ARG AZURE_OPENAI_API_KEY
ARG AZURE_OPENAI_DEPLOYMENT
ARG AZURE_OPENAI_EMBEDDING_DEPLOYMENT
ARG AZURE_OPENAI_API_VERSION
ARG JWT_SECRET
ARG METABASE_KEY
ARG MB_EMBED_SECRET
ARG MB_EMBED_URL
ARG DB_HOST=postgres
ARG DB_PORT=5432
ARG DB_NAME=unity_ai
ARG DB_USER=unity_user
ARG DB_PASSWORD=unity_pass

WORKDIR /app

# Install minimal system dependencies
RUN apt-get update && apt-get install -y \
    curl \
    postgresql-client \
    && rm -rf /var/lib/apt/lists/*

# Copy requirements first for better caching
COPY requirements.txt .

# Install Python dependencies
RUN pip install --no-cache-dir -r requirements.txt

# Copy the source code
COPY src/ .

# No startup script needed

# Expose port
EXPOSE 5000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/ || exit 1

# Set environment variables from build args
ENV PYTHONUNBUFFERED=1
ENV AZURE_OPENAI_ENDPOINT=${AZURE_OPENAI_ENDPOINT}
ENV AZURE_OPENAI_API_KEY=${AZURE_OPENAI_API_KEY}
ENV AZURE_OPENAI_DEPLOYMENT=${AZURE_OPENAI_DEPLOYMENT}
ENV AZURE_OPENAI_EMBEDDING_DEPLOYMENT=${AZURE_OPENAI_EMBEDDING_DEPLOYMENT}
ENV AZURE_OPENAI_API_VERSION=${AZURE_OPENAI_API_VERSION}
ENV JWT_SECRET=${JWT_SECRET}
ENV METABASE_KEY=${METABASE_KEY}
ENV MB_EMBED_SECRET=${MB_EMBED_SECRET}
ENV MB_EMBED_URL=${MB_EMBED_URL}
ENV DB_HOST=${DB_HOST}
ENV DB_PORT=${DB_PORT}
ENV DB_NAME=${DB_NAME}
ENV DB_USER=${DB_USER}
ENV DB_PASSWORD=${DB_PASSWORD}

# Run the modular backend
CMD ["python", "app.py"]