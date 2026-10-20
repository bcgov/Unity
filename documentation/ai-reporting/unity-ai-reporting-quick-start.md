# Unity AI Reporting - Quick Start

This document covers getting the Unity AI Reporting platform running locally and its container architecture.

## Quick Start

1. **Navigate to applications directory:**
```bash
cd applications
```

2. **Configure environment:**
```bash
cp .env.example .env
# Edit .env with your Azure OpenAI, JWT, and Metabase credentials
```

3. **Start the application:**
```bash
docker-compose up --build
```

4. **Access the application:**
   - Application: http://localhost
   - Testing with JWT: http://localhost/?token=YOUR_JWT_TOKEN

## Architecture

**Single Combined Container**: Flask serves both the Angular frontend (static files) and backend API endpoints.

```
Browser (localhost:80) → Flask (container port 8080)
                          ├── /api/* → Backend API
                          └── /*     → Angular static files
```
