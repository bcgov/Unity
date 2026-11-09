# Unity AI Applications - Deployment Guide

This directory contains the Unity AI Reporting platform with combined frontend and backend deployment.

## Architecture

**Single Combined Container**: Flask serves both the Angular frontend (static files) and backend API endpoints. No nginx required.

```
Browser (localhost:80) → Flask (container port 8080)
                          ├── /api/* → Backend API
                          └── /*     → Angular static files
```

## Quick Start

### Prerequisites
- Docker and Docker Compose
- `.env` file (copy from `.env.example`)
- `Unity.AI.Reporting.Backend/src/tenant_config.local.json` with the
  default tenant's Metabase API key (mounted at runtime, not included in the image):
  ```json
  {
    "Default Grants Program": {
      "api_key": "YOUR_METABASE_API_KEY"
    }
  }
  ```

### Run
From PowerShell on Windows:
```powershell
.\ai-startup.ps1
```

The script forwards the dev OpenShift AI proxy to localhost:18080 (reusing an
existing forward), passes `http://host.docker.internal:18080/` as the Azure
endpoint to Docker Compose, builds and starts the application, then opens
http://localhost. Log into OpenShift with `oc login --web` first. The proxy
forward remains running for AI queries after the script returns. The script
uses an optional, gitignored `docker-compose.override.yml` for machine-specific
settings. For local GrantManager authentication, the launcher uses `ReportingAI:JWTSecret` from
GrantManager's `appsettings.Development.json` rather than the AI `.env` key.
Open AI Reporting through GrantManager to authenticate; opening
`http://localhost` directly without a token shows the access-check screen.

For a local PostgreSQL host port of 5435 and a GrantManager parent at
`https://localhost:44342`, create `docker-compose.override.yml` in this
directory (Docker Compose 2.24.4 or newer is required for `!override`):
```yaml
services:
  postgres:
    ports: !override
      - "5435:5432"
  reporting:
    volumes:
      - type: bind
        source: ./Unity.AI.Reporting.Backend/src/tenant_config.local.json
        target: /app/backend/src/tenant_config.local.json
        read_only: true
        bind:
          create_host_path: false
    environment:
      - ORIGIN_URL=${ORIGIN_URL},https://localhost:44342
```
The launcher and shutdown script include this file when it exists. It is
gitignored and excluded from the Docker build context, so your local settings
will not be committed or baked into the image.

To use the `AZURE_OPENAI_ENDPOINT` from your `.env` instead of forwarding the
dev proxy, run `.\ai-startup.ps1 -UseConfiguredAzureEndpoint`. For standalone AI
testing without GrantManager, `-UseConfiguredJwtSecret` keeps the AI `.env`
signing key. To start without opening a browser, use Docker Compose directly:
```bash
docker compose up --build -d
```

Direct Docker Compose commands use the values in `.env` and automatically
load the local override when present; they do not set up the OpenShift proxy.
An OpenShift-only Azure service name will not resolve from local Docker. Use
`http://localhost/ready` to check whether the initial schema embedding has
finished; `/health` only checks that the web server is running.

### Stop
From PowerShell in this directory:
```powershell
.\ai-shutdown.ps1
```

This stops the AI containers and the dedicated local OpenShift proxy forward.
It preserves the PostgreSQL data volume and leaves unrelated processes on
port 18080 alone.

### Access
- **Application**: http://localhost
- **AI PostgreSQL from the host**: localhost:5432 by default, or localhost:5435
  with the local override above (containers always use postgres:5432).

## Configuration

Copy `.env.example` to `.env` and configure required variables:

### Critical Variables (Must Set)
```env
# Azure OpenAI — endpoint and key only; deployment names and versions are hardcoded in config.py
AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com/
AZURE_OPENAI_API_KEY=your_api_key

# Authentication
JWT_SECRET=your_jwt_secret_64_chars_minimum
ORIGIN_URL=https://your-parent-domain.com,http://localhost

# Metabase — API key lives in tenant_config.local.json (gitignored), not here
MB_URL=https://your-metabase-instance.com
MB_MAP_REGION_UUID=your_metabase_regional_districts_uuid

# Database — shared by app and postgres container
DB_NAME=unity_ai
DB_USER=unity_user
DB_PASSWORD=your_secure_password
```

### Platform Variables
```env
# Build info (baked into image)
UAI_BUILD_VERSION=0.1.0
UAI_BUILD_REVISION=0000000
UAI_TARGET_ENVIRONMENT=LocalDevelopment

# Flask environment
FLASK_ENV=development
```

### JWT Authentication Configuration
```env
# ORIGIN_URL configures iframe authentication security
# For local development, include localhost for URL token access
# For production, specify authorized parent domains for PostMessage
ORIGIN_URL=http://localhost

# Example configurations:
# Local only: ORIGIN_URL=http://localhost
# Production only: ORIGIN_URL=https://your-parent-domain.com
# Multi-environment: ORIGIN_URL=https://prod.domain.com,https://test.domain.com,http://localhost
```

See [Environment Configuration Guide](../documentation/unity-ai-reporting-environment-specific-configuration.md) for complete variable reference.

## Project Structure

```
applications/
├── Dockerfile                     # Combined frontend+backend
├── docker-compose.yml             # Local deployment
├── .env.example                   # Configuration template
├── Unity.AI.Reporting.Backend/    # Flask API + AI
│   └── src/
│       ├── api.py                 # API routes
│       ├── static_routes.py       # Serves Angular files
│       ├── sql_generator.py       # AI SQL generation
│       └── config.py             # Configuration manager
└── Unity.AI.Reporting.Frontend/   # Angular app
    └── src/app/
        ├── services/              # API services
        └── components/           # UI components
```

## Common Commands

```bash
# Start
docker-compose up

# Rebuild and start
docker-compose up --build

# View logs
docker-compose logs -f reporting

# Stop
docker-compose down

# Access database
docker-compose exec postgres psql -U unity_user -d unity_ai
```

## Deployment

### OpenShift Compatibility

✅ Runs as non-root user (UID 1001)  
✅ Uses port 8080 (non-privileged)  
✅ Single container deployment  
✅ Runtime configuration via environment variables

### Build & Push

```bash
# Build image
docker build -t unity-ai-reporting:latest .

# Tag for registry  
docker tag unity-ai-reporting:latest your-registry/unity-ai-reporting:latest

# Push
docker push your-registry/unity-ai-reporting:latest
```

### Environment-Specific Configuration

The application supports multiple deployment environments. Non-sensitive configuration (deployment names, model versions, DB IDs) is hardcoded in `config.py` or derived from `tenant_config.json`. Only secrets and base URLs need to be injected at runtime.

See [Environment Configuration Guide](../documentation/unity-ai-reporting-environment-specific-configuration.md) for details.

## Component Documentation

- [Backend README](./Unity.AI.Reporting.Backend/README.md) - API documentation and database setup
- [Frontend README](./Unity.AI.Reporting.Frontend/README.md) - Angular application details

## Troubleshooting

### Container won't start
```bash
docker-compose logs reporting
```

### Frontend can't reach backend
Verify the application is using the combined container architecture (Angular served by Flask)

### Database connection issues
```bash
docker-compose ps postgres
docker-compose exec reporting python -c "from src.config import config; print(config.database.url)"
```

### JWT authentication fails
Ensure `JWT_SECRET` is set and at least 64 characters long

### Missing environment variables
Check that all critical variable are set in your `.env` file

## Testing & Validation

### Authentication Testing
```bash
# 1. Test direct URL access with JWT token (localhost only)
# Ensure ORIGIN_URL includes "http://localhost" for this to work
http://localhost/?token=YOUR_JWT_TOKEN

# 2. Verify origin configuration
curl http://localhost/api/iframe-origins

# Expected response (includes localhost for local development):
# {"iframe_origins": ["http://localhost"]}

# 3. Check application health
curl http://localhost/api/health
```

### JWT Token Flow Testing
1. **Local Development**: Direct URL access works with `?token=` parameter
2. **Production Iframe**: PostMessage from authorized ORIGIN_URL domains only
3. **Security Validation**: Direct access blocked on production domains

### Environment Variable Verification
```bash
# Check ORIGIN_URL is loaded in container
docker-compose exec reporting printenv | grep ORIGIN_URL

# Should show: ORIGIN_URL=http://localhost
```

### Console Log Patterns for Debugging
- `AUTH SERVICE ORIGIN VALIDATION:` - Origin validation details
- `AI Reporting AUTH SERVICE: Origin validation passed` - Success
- `AI Reporting AUTH SERVICE: Rejected token` - Blocked origin
- `AUTH SERVICE: URL token loaded, authorized: true` - Local URL token
- `Token acknowledgment received` - PostMessage success