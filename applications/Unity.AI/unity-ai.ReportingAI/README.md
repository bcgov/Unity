# Metabase Reporter

A modular, extensible natural language to SQL application integrated with Metabase. Convert natural language queries into SQL, create visualizations, and manage conversations with multi-tenant support.

## Features

- 🤖 **Natural Language to SQL**: Convert plain English questions into SQL queries
- 📊 **Metabase Integration**: Automatic card creation and visualization
- 🏢 **Multi-Tenant Support**: Database isolation per tenant with db_id filtering
- 💬 **Conversation Management**: Save and restore chat histories
- 🔍 **Smart Schema Search**: Vector embeddings for relevant table discovery
- 🎯 **Majority Voting**: Multiple LLM samples for robust SQL generation
- 🔌 **Extensible Architecture**: Modular design for easy customization

## Project Structure

```
applications/
└── unity-ai.ReportingAI/
    ├── src/
    │   └── unity-ai.ReportingAI.Backend/
    │       └── src/                    # Modular Python backend
    │           ├── config.py          # Configuration management
    │           ├── database.py        # Database operations
    │           ├── metabase.py        # Metabase API client
    │           ├── embeddings.py      # Vector embeddings
    │           ├── chat.py            # Chat management
    │           ├── sql_generator.py   # NL to SQL conversion
    │           ├── api.py             # Flask API routes
    │           ├── app.py             # Main entry point
    │           ├── custom_fields.py   # Custom field utilities
    │           └── daily_job.py       # Scheduled tasks
    ├── .env                           # Environment configuration
    ├── requirements.txt               # Python dependencies
    ├── QDECOMP_examples.json         # NL-to-SQL training examples
    ├── sql_examples.json             # Additional SQL examples
    ├── docker-compose.yml            # Production orchestration
    ├── docker-compose.dev.yml        # Development orchestration
    ├── Dockerfile                    # Backend container definition
    └── README.md                     # This file
```

## Quick Start

### Local Development

1. **Clone the repository**
```bash
git clone <repository-url>
cd unity-ai
```

2. **Set up environment variables**
```bash
# Create .env file in the project root
cp .env.example .env
# Edit .env with your configuration
```

3. **Install dependencies**
```bash
pip install -r requirements.txt
```

4. **Initialize database**
```bash
cd src/unity-ai.ReportingAI.Backend/src
python app.py
# The database tables will be created automatically
```

5. **Embed database schemas**
```bash
cd src/unity-ai.ReportingAI.Backend/src
python app.py embed
# Or for a specific database:
python app.py embed 3
```

### Docker Deployment

#### Production
```bash
# Build and run all services
docker-compose up --build

# Embed schemas in the backend container
docker-compose exec backend python src/unity-ai.ReportingAI.Backend/src/app.py embed
```

#### Development
```bash
# Run with development configuration
docker-compose -f docker-compose.dev.yml up --build

# If you encounter dependency issues, force rebuild without cache:
docker-compose -f docker-compose.dev.yml build --no-cache
docker-compose -f docker-compose.dev.yml up

# Access services:
# - Backend: http://localhost:5000
# - pgAdmin: http://localhost:8080 (admin@example.com / admin)
```

## Configuration

### Environment Variables

Create a `.env` file in the project root:

```env
# Metabase Configuration
METABASE_KEY=mb_your-metabase-api-key
MB_EMBED_SECRET=your-embed-secret
MB_EMBED_URL=https://your-metabase-instance.com

# JWT Authentication
JWT_SECRET="your-super-secret-key-here"

# Azure OpenAI Configuration
AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com
AZURE_OPENAI_API_KEY=your-azure-openai-key
AZURE_OPENAI_DEPLOYMENT=gpt-4o-mini
AZURE_OPENAI_EMBEDDING_DEPLOYMENT=text-embedding-3-large
AZURE_OPENAI_API_VERSION=2024-02-01

# Database Configuration (for Docker)
DB_HOST=localhost
DB_PORT=5432
DB_NAME=unity_ai
DB_USER=unity_user
DB_PASSWORD=unity_pass

# Application Settings
FLASK_ENV=development
EMBED_WORKSHEETS=true
```

### Tenant Configuration

Add new tenants in `src/unity-ai.ReportingAI.Backend/src/config.py`:

```python
def _load_tenant_mappings(self):
    return {
        "YourOrg": {
            "db_id": 5,
            "collection_id": 100,
            "schema_types": ["public", "custom"]
        }
    }
```

## API Endpoints

### Query Processing
- `POST /api/ask` - Convert natural language to SQL and create Metabase card
  ```json
  {
    "question": "How many users signed up last month?",
    "conversation": [],
    "metabase_url": "https://metabase.example.com",
    "tenant_id": "YourOrg"
  }
  ```

### Visualization Management
- `POST /api/change_display` - Update card visualization type
- `POST /api/delete` - Delete a Metabase card

### Chat Management
- `POST /api/chats` - Get all chats for a user
- `POST /api/chats/<id>` - Get specific chat with card validation
- `POST /api/chats/save` - Save or update chat
- `DELETE /api/chats/<id>` - Delete chat

## CLI Commands

### Backend Commands
```bash
# Run the Flask server
cd src/unity-ai.ReportingAI.Backend/src
python app.py

# Embed database schemas
cd src/unity-ai.ReportingAI.Backend/src
python app.py embed [db_id]

# Show help
cd src/unity-ai.ReportingAI.Backend/src
python app.py help
```

## Extending the Application

### Adding New Use Cases

1. **Configure the tenant** in `src/unity-ai.ReportingAI.Backend/src/config.py`
2. **Customize schema extraction** in `src/unity-ai.ReportingAI.Backend/src/embeddings.py`
3. **Add domain-specific examples** to `QDECOMP_examples.json`
4. **Extend API endpoints** in `src/unity-ai.ReportingAI.Backend/src/api.py`

### Replacing Components

The modular architecture allows easy replacement of:
- **LLM Provider**: Modify `sql_generator.py`
- **BI Tool**: Replace `metabase.py` with your BI tool's API
- **Database**: Update `database.py` for different backends
- **Embeddings**: Change vector store in `embeddings.py`

## Development

### Running Tests
```bash
cd src/unity-ai.ReportingAI.Backend/src
python -m pytest tests/
```

### Code Structure

Each module is independent and focused:
- `config.py` - All configuration in one place
- `database.py` - Database operations only
- `metabase.py` - Metabase API interactions
- `embeddings.py` - Vector operations
- `chat.py` - Conversation logic
- `sql_generator.py` - NL to SQL logic
- `api.py` - HTTP endpoints
- `app.py` - Application bootstrap

## Production Deployment

### Using Docker

The application includes both backend and frontend containers:

```bash
# Production deployment
docker-compose up --build -d

# Backend only (for API development)
docker build -t metabase-reporter-backend .
docker run -p 5000:5000 --env-file .env metabase-reporter-backend

# Development with hot reload
docker-compose -f docker-compose.dev.yml up --build
```

### Health Checks

- Health endpoint: `GET /`
- Returns: "Backend is working!"

### Scaling Considerations

- Use PostgreSQL connection pooling
- Deploy multiple Flask workers with Gunicorn
- Cache embeddings for frequently accessed schemas
- Use Redis for session management (if needed)

## Troubleshooting

### Common Issues

1. **Environment variables not loaded**
   - Ensure `.env` file is in project root
   - Verify all required environment variables are set
   - Check Azure OpenAI configuration if using Azure

2. **Module import errors (e.g., "ModuleNotFoundError: No module named 'dotenv'")**
   - Force rebuild without cache: `docker-compose -f docker-compose.dev.yml build --no-cache`
   - This ensures the latest requirements.txt is used
   - Then run: `docker-compose -f docker-compose.dev.yml up`

3. **SQL generation fails**
   - Verify Azure OpenAI configuration in `.env`
   - Check QDECOMP_examples.json exists in project root
   - Review container logs: `docker-compose logs backend`

4. **Metabase cards not created**
   - Verify METABASE_KEY has sufficient permissions
   - Check collection_id exists in Metabase
   - Ensure MB_EMBED_SECRET is correct in `.env`

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

[Your License Here]

## Support

For issues and questions:
- Create an issue on GitHub
- Contact the development team
- Check the documentation