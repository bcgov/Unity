# Unity AI Platform - Applications

AI-powered reporting platform with natural language to SQL conversion and Metabase integration.

## Quick Start

### Prerequisites
- Docker and Docker Compose
- `.env` file with required configuration

### Development
```bash
docker-compose -f docker-compose.dev.yml up
```

### Production
```bash
docker-compose up -d
```

## Services

- **Frontend**: http://localhost:80 (Angular 20)
- **Backend API**: http://localhost:5000 (Flask + Azure OpenAI)
- **Database**: PostgreSQL with pgvector
- **pgAdmin** (dev only): http://localhost:8080

## Configuration

Copy `.env.example` to `.env` and configure:

```env
# Azure OpenAI
AZURE_OPENAI_ENDPOINT=your_endpoint
AZURE_OPENAI_API_KEY=your_key
AZURE_OPENAI_DEPLOYMENT=gpt-4o-mini
AZURE_OPENAI_EMBEDDING_DEPLOYMENT=text-embedding-3-large

# JWT
JWT_SECRET=your_secret_key

# Metabase
METABASE_KEY=your_api_key
MB_EMBED_SECRET=your_embed_secret
MB_URL=https://your-metabase-url.com

# Database (Docker defaults)
POSTGRES_DB=unity_ai
POSTGRES_USER=unity_user
POSTGRES_PASSWORD=secure_password
```

## Project Structure

```
applications/
├── Unity.AI.Reporting.Backend/    # Flask API + AI SQL generation
├── Unity.AI.Reporting.Frontend/   # Angular chat interface
├── Unity.AI.Assessment.*/         # Future: assessment features
├── docker-compose.yml             # Production setup
├── docker-compose.dev.yml         # Development setup
└── .env.example                   # Configuration template
```

## Features

- Natural language to SQL conversion
- AI-powered query explanations
- Chat history management
- Admin feedback dashboard
- Multi-tenant support
- JWT authentication with roles

## Documentation

- [Backend README](./Unity.AI.Reporting.Backend/README.md)
- [Frontend README](./Unity.AI.Reporting.Frontend/README.md)

## Common Commands

```bash
# Rebuild after dependency changes
docker-compose -f docker-compose.dev.yml up --build

# View logs
docker-compose logs -f backend

# Embed database schemas (after first run)
docker-compose exec reporting-backend python app.py embed
```
