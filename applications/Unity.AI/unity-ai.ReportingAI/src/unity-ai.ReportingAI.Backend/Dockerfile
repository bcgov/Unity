FROM python:3.11-slim

# Accept build arguments
ARG OPENAI_API_KEY
ARG JWT_SECRET
ARG METABASE_URL
ARG METABASE_USERNAME
ARG METABASE_PASSWORD
ARG DB_HOST=postgres
ARG DB_PORT=5432
ARG DB_NAME=unity_ai
ARG DB_USER=unity_user
ARG DB_PASSWORD=unity_pass

WORKDIR /app

# Install minimal system dependencies
RUN apt-get update && apt-get install -y \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Copy requirements first for better caching
COPY requirements.txt .

# Install Python dependencies
RUN pip install --no-cache-dir -r requirements.txt

# Copy the source code
COPY src/ .

# Expose port
EXPOSE 5000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/ || exit 1

# Set environment variables from build args
ENV PYTHONUNBUFFERED=1
ENV OPENAI_API_KEY=${OPENAI_API_KEY}
ENV JWT_SECRET=${JWT_SECRET}
ENV METABASE_URL=${METABASE_URL}
ENV METABASE_USERNAME=${METABASE_USERNAME}
ENV METABASE_PASSWORD=${METABASE_PASSWORD}
ENV DB_HOST=${DB_HOST}
ENV DB_PORT=${DB_PORT}
ENV DB_NAME=${DB_NAME}
ENV DB_USER=${DB_USER}
ENV DB_PASSWORD=${DB_PASSWORD}

# Run the modular backend
CMD ["python", "app.py"]