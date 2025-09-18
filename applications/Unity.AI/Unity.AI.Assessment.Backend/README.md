# Unity AI Assessment Backend

Python Flask backend service for the Unity AI Assessment system. Provides AI-powered assessment, evaluation, and scoring capabilities with intelligent automation.

## Docker Development

From the application root directory:
```bash
docker-compose -f docker-compose.dev.yml up assessment-backend
```

## Building

To build the Docker image:
```bash
docker build -t unity-ai-assessment-backend .
```