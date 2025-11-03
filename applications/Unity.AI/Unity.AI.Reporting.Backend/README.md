# Unity AI Reporting - Backend

Python Flask backend service providing AI-powered SQL query generation and Metabase integration for natural language data queries.

## Features

- Natural language to SQL conversion using Azure OpenAI
- Metabase API integration for visualization
- PostgreSQL with pgvector for schema embeddings
- JWT authentication with admin roles
- Chat conversation management
- User feedback and bug reporting system

## Tech Stack

- **Framework**: Flask 3.1.1
- **Database**: PostgreSQL with psycopg (binary)
- **AI/ML**: Azure OpenAI (GPT-4o-mini), LangChain
- **Authentication**: JWT (PyJWT)
- **Logging**: Python logging module

## Quick Start

### Using Docker Compose (Recommended)

From the `applications` directory:

```bash
# Development
docker-compose -f docker-compose.dev.yml up

# Production
docker-compose up -d
```

### Local Development

1. Install dependencies:
```bash
pip install -r requirements.txt
```

2. Set environment variables (see .env.example in applications directory)

3. Run the application:
```bash
python app.py
```

Server runs on `http://localhost:5000`

## Environment Variables

Required variables (see `applications/.env.example`):

```env
# Azure OpenAI
AZURE_OPENAI_ENDPOINT=your_endpoint_here
AZURE_OPENAI_API_KEY=your_key_here
AZURE_OPENAI_DEPLOYMENT=gpt-4o-mini
AZURE_OPENAI_EMBEDDING_DEPLOYMENT=text-embedding-3-large
AZURE_OPENAI_API_VERSION=2024-02-01

# JWT Authentication
JWT_SECRET=your_secure_secret_key

# Metabase
METABASE_KEY=your_metabase_api_key
MB_EMBED_SECRET=your_metabase_embed_secret
MB_URL=https://your-metabase-url.com
DEFAULT_EMBED_DB_ID=1

# Database (defaults provided for Docker)
DB_HOST=postgres
DB_PORT=5432
DB_NAME=unity_ai
DB_USER=unity_user
DB_PASSWORD=your_secure_password
```

**Security Note**: Never pass secrets as Docker build arguments. Use runtime environment variables only.

## Key API Endpoints

### Public
- `GET /` - Health check
- `GET /health` - Service health status
- `GET /ready` - Readiness check with dependencies

### Authenticated
- `POST /api/ask` - Generate SQL from natural language
- `POST /api/explain_sql` - Get SQL explanation
- `POST /api/change_display` - Update visualization
- `POST /api/chats` - Get user's chats
- `GET /api/chats/<chat_id>` - Get specific chat
- `POST /api/chats/save` - Save/update chat
- `DELETE /api/chats/<chat_id>` - Delete chat
- `POST /api/feedback` - Submit bug report/feedback

### Admin Only
- `GET /api/admin/feedback` - Get all feedback entries
- `GET /api/feedback/<feedback_id>` - Get specific feedback
- `PUT /api/admin/feedback/<feedback_id>/status` - Update feedback status

## Key Files

- `app.py` - Application entry point
- `api.py` - Flask routes and endpoints
- `auth.py` - JWT authentication
- `config.py` - Configuration management
- `database.py` - Database operations and repositories
- `sql_generator.py` - AI-powered SQL generation
- `metabase.py` - Metabase API client
- `embeddings.py` - Schema embedding management
- `chat.py` - Chat conversation management

## Database Tables

- `chats` - User conversation history
- `feedback` - User feedback and bug reports
- `langchain_pg_collection` - Vector store collections
- `langchain_pg_embedding` - Schema embeddings

## Commands

```bash
# Run server
python app.py

# Embed database schemas
python app.py embed [db_id]

# Show help
python app.py help
```

## Logging

The application uses Python's built-in logging module with proper log levels:
- `DEBUG` - Development mode only
- `INFO` - General information
- `WARNING` - Warning messages
- `ERROR` - Error messages with stack traces

Log format: `%(asctime)s - %(name)s - %(levelname)s - %(message)s`
