# Unity AI Reporting - Backend

Python Flask backend service providing AI-powered SQL query generation and Metabase integration for natural language data queries.

## Deployment

**Note**: The backend is deployed as part of a **combined container** with the frontend. Flask serves both the API endpoints and Angular static files. See the [main README](../README.md) for deployment instructions.

## Features

- Natural language to SQL conversion using Azure OpenAI
- Metabase API integration for visualization
- PostgreSQL with PGVector for schema embeddings
- JWT authentication with admin roles
- Chat conversation management
- User feedback and bug reporting system
- **Static file serving** for Angular frontend

## Tech Stack

- **Framework**: Flask 3.1.1
- **WSGI Server**: Gunicorn (production)
- **Database**: PostgreSQL with psycopg (binary), PGVector
- **AI/ML**: Azure OpenAI (GPT-4o-mini), LangChain
- **Authentication**: JWT (PyJWT)
- **Logging**: Python logging module

## Quick Start

### Using Docker (Recommended)

From the `applications` directory:

```bash
docker-compose up --build
```

Access at http://localhost

### Local Development

1. Install dependencies:
```bash
pip install -r requirements.txt
```

2. Set environment variables (see `applications/.env.example`)

3. Run the application:
```bash
cd src
python app.py
```

Server runs on `http://localhost:5000`

## Environment Variables

Required variables (see `applications/.env.example`):

```env
# Azure OpenAI
AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com/
AZURE_OPENAI_API_KEY=your_key
AZURE_OPENAI_DEPLOYMENT=gpt-4o-mini
AZURE_OPENAI_EMBEDDING_DEPLOYMENT=text-embedding-3-large
AZURE_OPENAI_API_VERSION=2024-02-15-preview

# JWT Authentication
JWT_SECRET=your_secure_secret_key

# Metabase
METABASE_KEY=your_metabase_api_key
MB_EMBED_SECRET=your_metabase_embed_secret
MB_URL=https://your-metabase-url.com
DEFAULT_EMBED_DB_ID=3

# Database
DB_HOST=postgres
DB_PORT=5432
DB_NAME=unity_ai
DB_USER=unity_user
DB_PASSWORD=your_secure_password
```

**Security**: Never pass secrets as Docker build arguments. Use runtime environment variables only.

## Key API Endpoints

### Public
- `GET /` - Health check
- `GET /health` - Service health status
- `GET /ready` - Readiness check with dependencies

### Static Files (Combined Container)
- `GET /*` - Serves Angular static files (handled by `static_routes.py`)

### Authenticated
- `POST /api/ask` - Generate SQL from natural language
- `POST /api/validate-token` - Validate JWT token
- `POST /api/check-admin` - Check admin privileges
- `POST /api/explain_sql` - Get SQL explanation
- `POST /api/change_display` - Update visualization
- `POST /api/delete` - Delete Metabase card
- `POST /api/chats` - Get user's chats
- `GET /api/chats/<chat_id>` - Get specific chat
- `POST /api/chats/save` - Save/update chat
- `DELETE /api/chats/<chat_id>` - Delete chat
- `POST /api/feedback` - Submit bug report/feedback
- `GET /api/metabase-url` - Get Metabase URL

### Admin Only
- `GET /api/admin/feedback` - Get all feedback entries
- `GET /api/feedback/<feedback_id>` - Get specific feedback  
- `PUT /api/admin/feedback/<feedback_id>/status` - Update feedback status

## Key Files

- `app.py` - Application entry point, CLI commands
- `api.py` - Flask routes and endpoints
- `static_routes.py` - **NEW**: Serves Angular static files
- `auth.py` - JWT authentication middleware
- `config.py` - Configuration management
- `database.py` - Database operations and repositories
- `sql_generator.py` - AI-powered SQL generation
- `metabase.py` - Metabase API client
- `embeddings.py` - Schema embedding with PGVector
- `chat.py` - Chat conversation management

## Database Tables

- `chats` - User conversation history
- `feedback` - User feedback and bug reports
- `langchain_pg_collection` - Vector store collections (PGVector)
- `langchain_pg_embedding` - Schema embeddings (PGVector)

## Commands

```bash
# Run server
python app.py

# Embed database schemas
python app.py embed [db_id]

# Show help
python app.py help
```

In Docker:
```bash
# Embed schemas
docker-compose exec reporting python app.py embed 3
```

## Architecture

### Combined Container Setup

Flask serves both:
1. **API Endpoints** (`/api/*`) - Backend logic
2. **Static Files** (`/*`) - Angular frontend

**How it works:**
- `static_routes.py` registers catch-all route
- Routes starting with `api/` → Flask API handlers
- All other routes → Angular `index.html` (client-side routing)
- No nginx required

### Vector Embeddings

Database schemas are embedded using Azure OpenAI and stored in PGVector:

1. Schema extraction from Metabase
2. Embedding generation (text-embedding-3-large)
3. Storage in PostgreSQL with PGVector extension
4. Similarity search during SQL generation

## Logging

Python's built-in logging with proper levels:
- `DEBUG` - Development mode only
- `INFO` - General information
- `WARNING` - Warning messages
- `ERROR` - Error messages with stack traces

Format: `%(asctime)s - %(name)s - %(levelname)s - %(message)s`

## CORS

CORS enabled for frontend-backend communication (configured in `api.py`).

## Security

- JWT authentication on all `/api/*` endpoints
- Admin role verification via `is_it_admin` claim
- SQL injection prevention via parameterized queries
- Input sanitization in `api.py`
- No secrets in build arguments
