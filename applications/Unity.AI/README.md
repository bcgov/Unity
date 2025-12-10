# Unity AI Platform - Applications

AI-powered reporting platform with natural language to SQL conversion and Metabase integration.

## Architecture

**Single Combined Container**: Flask serves both the Angular frontend (static files) and backend API endpoints. No nginx required.

```
Browser → Flask (port 80)
          ├── /api/* → Backend API
          └── /*     → Angular static files
```

## Quick Start

### Prerequisites
- Docker and Docker Compose
- `.env` file (copy from `.env.example`)

### Run
```bash
# Start services
docker-compose up --build

# Or run in background
docker-compose up -d
```

### Access
- **Application**: http://localhost

## Configuration

Copy `.env.example` to `.env` and configure:

```env
# Platform
UAI_BUILD_VERSION=1.0.0
ENVIRONMENT=production

# Azure OpenAI
AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com/
AZURE_OPENAI_API_KEY=your_key
AZURE_OPENAI_DEPLOYMENT=gpt-4o-mini
AZURE_OPENAI_EMBEDDING_DEPLOYMENT=text-embedding-3-large
AZURE_OPENAI_API_VERSION=2024-02-15-preview

# Authentication
JWT_SECRET=your_secure_secret

# Metabase
METABASE_KEY=your_api_key
MB_EMBED_SECRET=your_embed_secret
MB_URL=https://your-metabase-url.com
DEFAULT_EMBED_DB_ID=3

# Database (Docker defaults)
POSTGRES_DB=unity_ai
POSTGRES_USER=unity_user
POSTGRES_PASSWORD=secure_password
```

## Project Structure

```
applications/
├── Dockerfile                     # Combined frontend+backend
├── docker-compose.yml             # Deployment config
├── Unity.AI.Reporting.Backend/    # Flask API + AI
│   └── src/
│       ├── api.py                 # API routes
│       ├── static_routes.py       # Serves Angular files
│       └── sql_generator.py       # AI SQL generation
└── Unity.AI.Reporting.Frontend/   # Angular app
    └── src/app/services/
        ├── api.service.ts         # API client
        └── config.service.ts      # Runtime config
```

## Features

- Natural language to SQL conversion (Azure OpenAI)
- AI-powered query explanations  
- Chat history management
- Admin feedback dashboard
- Multi-tenant support
- JWT authentication with role-based access
- PGVector for schema similarity search

## Documentation

- [Backend README](./Unity.AI.Reporting.Backend/README.md) - API, database
- [Frontend README](./Unity.AI.Reporting.Frontend/README.md) - Angular app

## Common Commands

```bash
# Start
docker-compose up

# Rebuild
docker-compose up --build

# View logs
docker-compose logs -f reporting

# Stop
docker-compose down

# Embed database schemas (first run)
docker-compose exec reporting python app.py embed 3

# Access PostgreSQL
docker-compose exec postgres psql -U unity_user -d unity_ai
```

## Deployment

### OpenShift Compatibility

✅ Runs as non-root user (UID 1001)  
✅ Uses port 8080 (non-privileged)  
✅ Single container deployment  
✅ Runtime configuration via env vars

### Build & Deploy

```bash
# Build
docker build -t unity-ai-reporting:latest .

# Tag for registry
docker tag unity-ai-reporting:latest your-registry/unity-ai-reporting:latest

# Push
docker push your-registry/unity-ai-reporting:latest
```

See [deployment documentation](../documentation/manual-image-build-push-openshift.md) for details.

## Tech Stack

- **Frontend**: Angular 20, Material UI, PrimeNG
- **Backend**: Flask 3.1.1, Gunicorn, Azure OpenAI, LangChain
- **Database**: PostgreSQL 16 with PGVector
- **Container**: Docker (multi-stage build)

## How It Works

1. **Build**: Angular compiled to static files with `config.json` generated at build time
2. **Runtime**: Flask serves both static files and API endpoints via `static_routes.py`
3. **Configuration**: Angular loads build-time `config.json` with version/environment info
4. **Requests**: Angular calls `/api/*` → same origin, no CORS issues

## Troubleshooting

### Container won't start
```bash
docker-compose logs reporting
```

### Frontend can't reach backend
Verify `apiUrl` defaults to `/api` in config.service.ts

### Database issues
```bash
docker-compose ps postgres
docker-compose exec reporting python -c "from database import db_manager; print(db_manager)"
```

### Token validation fails
Check `JWT_SECRET` environment variable is set
