# Unity AI Backend

Python Flask backend service for the Unity AI Reporting system. Provides AI-powered SQL query generation and integration with Metabase.

## Features

- Natural language to SQL conversion using OpenAI
- Metabase API integration for dashboard management
- PostgreSQL database with pgvector for embeddings
- JWT authentication
- CORS support for frontend integration
- Schema embeddings for intelligent query generation

## Tech Stack

- **Framework**: Flask 3.1.1
- **Database**: PostgreSQL with psycopg[binary]
- **AI/ML**: OpenAI API, LangChain, Hugging Face
- **Authentication**: Flask-JWT-Extended
- **Environment**: Python-dotenv

## API Endpoints

- `GET /` - Health check
- `POST /chat` - Natural language query processing
- `POST /generate-sql` - Direct SQL generation
- `GET /metabase/*` - Metabase API proxy endpoints

## Development Setup

### Local Development

1. Navigate to backend directory:
```bash
cd src/unity-ai.ReportingAI.Backend
```

2. Create virtual environment:
```bash
python -m venv venv
source venv/bin/activate  # On Windows: venv\Scripts\activate
```

3. Install dependencies:
```bash
pip install -r requirements.txt
```

4. Set environment variables (see .env file in application root)

5. Run development server:
```bash
python app.py
```

### Docker Development

From the application root directory:
```bash
docker-compose -f docker-compose.dev.yml up backend
```

## Environment Variables

Required environment variables (set via build args or environment):

```env
# OpenAI Configuration
OPENAI_API_KEY=your_openai_api_key

# JWT Configuration
JWT_SECRET=your_jwt_secret

# Metabase Integration
METABASE_URL=your_metabase_url
METABASE_USERNAME=your_metabase_username
METABASE_PASSWORD=your_metabase_password

# Database Configuration (defaults provided)
DB_HOST=postgres
DB_PORT=5432
DB_NAME=unity_ai
DB_USER=unity_user
DB_PASSWORD=unity_pass
```

### Using Build Args

The Docker setup now uses build arguments to pass environment variables:

```bash
docker build --build-arg OPENAI_API_KEY="your_key" \
             --build-arg JWT_SECRET="your_secret" \
             --build-arg METABASE_URL="your_url" \
             .
```

Or with docker-compose (which reads from environment variables or .env file):

```bash
export OPENAI_API_KEY="your_key"
docker-compose up --build
```

## Key Components

- `app.py` - Main Flask application
- `chat.py` - Chat and query processing logic
- `sql_generator.py` - SQL generation from natural language
- `metabase.py` - Metabase API integration
- `database.py` - Database connection and operations
- `embeddings.py` - Schema embeddings management
- `config.py` - Configuration management

## Database Schema

The backend manages schema embeddings and query history in PostgreSQL with the pgvector extension for similarity search.