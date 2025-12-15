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

Copy `.env.example` to `.env` and configure required variables:

### Critical Variables (Must Set)
```env
# Azure OpenAI
AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com/
AZURE_OPENAI_API_KEY=your_api_key
AZURE_OPENAI_DEPLOYMENT=gpt-4o-mini
AZURE_OPENAI_EMBEDDING_DEPLOYMENT=text-embedding-3-large

# Authentication  
JWT_SECRET=your_jwt_secret_64_chars_minimum

# Metabase
METABASE_KEY=your_metabase_api_key
MB_EMBED_SECRET=your_metabase_embed_secret
MB_URL=https://your-metabase-instance.com
MB_EMBED_ID=5

# Database
POSTGRES_PASSWORD=your_secure_password
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

See [Environment Configuration Guide](./documentation/environment-specific-configuration.md) for complete variable reference.

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

The application supports multiple deployment environments with different configurations:

- **Development**: Local development with debug enabled
- **Test**: Testing environment with test database (DB_ID=3)
- **UAT**: User acceptance testing (DB_ID=5, FLASK_ENV=staging)  
- **Production**: Production environment (DB_ID=3, FLASK_ENV=production)

See [Environment Configuration Guide](./documentation/environment-specific-configuration.md) for details.

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