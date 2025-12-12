# Environment-Specific Configuration Guide

## Overview

The Unity AI platform requires environment-specific configuration for different deployment environments (dev, dev2, test, uat, prod). This guide outlines the variables that must be configured per environment.

## Critical Environment-Specific Variables

### 1. Metabase Database Configuration

These variables map to specific Metabase database instances in each environment:

| Variable | Purpose | Example Values |
|----------|---------|----------------|
| `DEFAULT_EMBED_DB_ID` | Default tenant database mapping | dev: "1", test: "4", prod: "6" |
| `MB_EMBED_ID` | Metabase metadata API database ID | Usually same as DEFAULT_EMBED_DB_ID |
| `MB_URL` | Metabase instance URL | Environment-specific Metabase endpoints |

### 2. Configuration Strategy

#### Baked into Docker Image (Dockerfile ENV)
These values are built into the container image as defaults:
```dockerfile
ENV DEFAULT_EMBED_DB_ID=1
ENV EMBED_WORKSHEETS=true
```

#### GitOps Environment Variables (Per Environment)
These are configured per environment via StatefulSet:

##### Development Environment
```bash
MB_EMBED_ID="1"
AI_MODEL="gpt-4o-mini"
EMBEDDING_MODEL="text-embedding-3-large"
# MB_URL configured via Vault secrets
```

##### Test Environment  
```bash
MB_EMBED_ID="4"
AI_MODEL="gpt-4o-mini"
EMBEDDING_MODEL="text-embedding-3-large"
# MB_URL configured via Vault secrets
```

##### Production Environment
```bash
MB_EMBED_ID="6"
AI_MODEL="gpt-4o-mini"
EMBEDDING_MODEL="text-embedding-3-large"
# MB_URL configured via Vault secrets
```

## OpenShift Implementation

### For Each Environment Namespace

Add only the environment-specific variables to your OpenShift StatefulSet:

```yaml
spec:
  template:
    spec:
      containers:
      - name: unity-ai-backend
        env:
        # Environment-specific Metabase database ID (required)
        - name: MB_EMBED_ID
          value: "1"  # Change per environment: dev="1", test="4", prod="6"
        
        # Optional AI model configuration (can override image defaults)
        - name: AI_MODEL
          value: "gpt-4o-mini"
        - name: EMBEDDING_MODEL
          value: "text-embedding-3-large"
        
        # Existing secret references (unchanged)
        - name: AZURE_OPENAI_ENDPOINT
          valueFrom:
            secretKeyRef:
              name: dev-unity-ai-secrets
              key: AZURE_OPENAI_ENDPOINT
        # ... other secrets remain the same
```

### GitOps Configuration Commands

For each environment, update the StatefulSet with only the required variables:

```bash
# Development environment
oc set env statefulset/dev-unity-ai-backend \
  MB_EMBED_ID="1" \
  AI_MODEL="gpt-4o-mini" \
  EMBEDDING_MODEL="text-embedding-3-large" \
  -n d18498-dev

# Test environment  
oc set env statefulset/test-unity-ai-backend \
  MB_EMBED_ID="4" \
  AI_MODEL="gpt-4o-mini" \
  EMBEDDING_MODEL="text-embedding-3-large" \
  -n d18498-test

# Production environment
oc set env statefulset/prod-unity-ai-backend \
  MB_EMBED_ID="6" \
  AI_MODEL="gpt-4o-mini" \
  EMBEDDING_MODEL="text-embedding-3-large" \
  -n d18498-prod
```

## How to Determine Your Environment's Database IDs

### Method 1: Check Metabase Admin Interface
1. Log into your environment's Metabase instance
2. Go to Settings → Admin → Databases
3. Note the database ID number in the URL or database list

### Method 2: API Query
```bash
curl -H "x-api-key: YOUR_METABASE_KEY" \
  https://your-metabase-url/api/database
```

### Method 3: Application Logs
Check application logs for database connection attempts to identify the correct ID.

## Configuration Validation

### Test Database Connectivity
After configuring, verify the backend can connect:

```bash
# Check application logs
oc logs deployment/unity-ai-backend-deployment -n your-namespace

# Look for successful Metabase API calls
# Should see successful responses from /api/database/{MB_EMBED_ID}/metadata
```

### Verify Tenant Mapping
The application uses these values in `config.py` tenant mappings:

```python
"default": {
    "db_id": int(os.getenv("DEFAULT_EMBED_DB_ID", "5")),
    "collection_id": 19,
    "schema_types": ["public"]
}
```

## Common Issues

### Issue: Application fails with "database not found"
**Solution**: Verify `MB_EMBED_ID` matches an actual database in your Metabase instance

### Issue: Tenant mapping uses wrong database
**Solution**: Ensure `DEFAULT_EMBED_DB_ID` is set correctly for your environment

### Issue: API calls fail with authentication errors
**Solution**: Verify `METABASE_KEY` has access to the specified database ID

## Local Development

For local development using docker-compose, the `.env` file contains default values:

```bash
# Local development values
DEFAULT_EMBED_DB_ID="1"
MB_EMBED_ID="1"
MB_URL="https://test-unity-reporting.apps.silver.devops.gov.bc.ca"
```

These will be overridden by environment-specific values in OpenShift deployments.