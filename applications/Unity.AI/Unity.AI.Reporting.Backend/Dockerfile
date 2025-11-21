FROM python:3.11-slim

# Platform build metadata (safe to bake into image)
ARG UAI_BUILD_VERSION=0.0.0
ARG UAI_BUILD_REVISION=0000000

# NOTE: No ARG statements for secrets/configuration
# All configuration is handled at runtime via environment variables
# This allows OpenShift StatefulSet to inject secrets and config without rebuilding images

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

# Set Python environment variables (non-sensitive configuration)
ENV PYTHONUNBUFFERED=1

# Set platform build metadata environment variables
ENV UNITY_BUILD_VERSION=${UAI_BUILD_VERSION}
ENV UAI_BUILD_REVISION=${UAI_BUILD_REVISION}

# NOTE: All application configuration (secrets, database, API keys) is provided at runtime
# via environment variables from OpenShift StatefulSet, Docker Compose, or other orchestrators.
# This prevents secrets from being baked into the container image and allows the same image
# to be deployed across dev/test/prod with different configurations.

# Run the modular backend
CMD ["python", "app.py"]