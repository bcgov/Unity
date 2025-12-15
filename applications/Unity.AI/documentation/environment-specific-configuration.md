# Environment-Specific Configuration Guide

## Overview

The Unity AI platform requires environment-specific configuration for different deployment environments (dev, test, uat, prod). This guide provides a comprehensive reference for all required environment variables and their proper configuration.

**GitOps Repository Structure:**
```
tenant-gitops-d18498/manifests/uai/overlays/
├── dev/     # d18498-dev namespace
├── test/    # d18498-test namespace  
├── uat/     # d18498-test namespace (UAT shares test namespace)
└── prod/    # d18498-prod namespace
```

## Complete Environment Variables Reference

### 🔴 Critical Variables (No Defaults - Must Be Set)

| Variable | Purpose | Example Values |
|----------|---------|----------------|
| `AZURE_OPENAI_ENDPOINT` | Azure OpenAI service endpoint | dev: "https://d837ad-dev-econ-llm-east.openai.azure.com/" |
| `AZURE_OPENAI_API_KEY` | Environment-specific API key | dev: "dev_key_123", prod: "prod_key_789" |
| `AZURE_OPENAI_DEPLOYMENT` | Model deployment name | "gpt-4o-mini" or "gpt-4-32k" |
| `AZURE_OPENAI_EMBEDDING_DEPLOYMENT` | Azure OpenAI embeddings deployment | "text-embedding-3-large" |
| `JWT_SECRET` | JWT signing secret (must be unique per env) | Generate per env: `openssl rand -base64 64` |
| `METABASE_KEY` | Metabase API key for integration | "mb_AjOFDhFjcM/i/..." |
| `MB_EMBED_SECRET` | Metabase embedding secret | "548ca570ce8203..." |
| `MB_URL` | Metabase instance URL | dev: "https://dev-unity-reporting.apps.silver.devops.gov.bc.ca" |
| `MB_EMBED_ID` | Metabase database ID for queries | dev: "5", test: "3", uat: "5", prod: "3" |
| `POSTGRES_PASSWORD` | Database password | Environment-specific secure password |

### 🟡 Important Variables (Has Defaults - Environment-Specific)

| Variable | Purpose | Default Value | Environment-Specific Values |
|----------|---------|---------------|----------------------------|
| `FLASK_ENV` | Flask environment mode | "development" | dev=development, test=test, uat=staging, prod=production |
| `POSTGRES_DB` | Database name | "unity_ai" | Different per environment |
| `POSTGRES_USER` | Database username | "unity_user" | Different per environment |
| `DEFAULT_EMBED_DB_ID` | Default tenant database mapping | "5" | dev: "5", test: "3", uat: "5", prod: "3" |
| `AZURE_OPENAI_API_VERSION` | Azure OpenAI API version | "2024-02-01" | Could vary per environment |

### 🟢 Optional Variables (Has Defaults)

| Variable | Purpose | Default Value |
|----------|---------|---------------|
| `AI_MODEL` | Primary AI model | "gpt-4o-mini" |
| `EMBEDDING_MODEL` | Embedding model | "text-embedding-3-large" |
| `EMBED_WORKSHEETS` | Enable worksheet embedding | "true" |
| `DB_HOST` | Database host | "localhost" |
| `DB_PORT` | Database port | "5432" |
| `DB_NAME` | Database name (fallback) | "unity_ai" |
| `DB_USER` | Database user (fallback) | "unity_user" |
| `DB_PASSWORD` | Database password (fallback) | "unity_pass" |

### 🔧 Build-Time Variables (Baked into Docker Image)

| Variable | Purpose | Default | Usage |
|----------|---------|---------|-------|
| `UAI_BUILD_VERSION` | Application version | "0.0.0" | Frontend build-info.json |
| `UAI_BUILD_REVISION` | Git commit hash | "0000000" | Frontend build-info.json |
| `UAI_TARGET_ENVIRONMENT` | Target environment | "Development" | Frontend build-info.json |

### 🔵 Legacy/Unused Variables (Optional)

| Variable | Purpose | Default Value | Status |
|----------|---------|---------------|--------|
| `COMPLETION_ENDPOINT` | Legacy OpenAI endpoint | "" | Optional fallback |
| `COMPLETION_KEY` | Legacy OpenAI key | "" | Optional fallback |

## Actual GitOps Environment Configuration

### Current Environment Structure

Based on `tenant-gitops-d18498/manifests/uai/overlays/`:

| Environment | Namespace | FLASK_ENV | DEFAULT_EMBED_DB_ID | MB_URL |
|-------------|-----------|-----------|---------------------|---------|
| **dev** | d18498-dev | development | "5" | dev-unity-reporting.apps.silver.devops.gov.bc.ca |
| **test** | d18498-test | test | "3" | test-unity-reporting.apps.silver.devops.gov.bc.ca |
| **uat** | d18498-test | staging | "5" | uat-unity-reporting.apps.silver.devops.gov.bc.ca |
| **prod** | d18498-prod | production | "3" | unity-reporting.apps.silver.devops.gov.bc.ca |

## Environment Configuration Templates

### Development Environment

```bash
# 🔴 Critical (Must Set)
AZURE_OPENAI_ENDPOINT="https://d837ad-dev-econ-llm-east.openai.azure.com/"
AZURE_OPENAI_API_KEY="dev-api-key-here"
AZURE_OPENAI_DEPLOYMENT="gpt-4o-mini"
AZURE_OPENAI_EMBEDDING_DEPLOYMENT="text-embedding-3-large"
JWT_SECRET="dev-jwt-secret-64-chars-minimum"
METABASE_KEY="dev-metabase-api-key"
MB_EMBED_SECRET="dev-metabase-embed-secret"
MB_URL="https://dev-unity-reporting.apps.silver.devops.gov.bc.ca"
MB_EMBED_ID="5"
POSTGRES_PASSWORD="dev-postgres-password"

# 🟡 Environment-Specific
FLASK_ENV="development"
POSTGRES_DB="unity_ai_dev"
POSTGRES_USER="dev_user"
DEFAULT_EMBED_DB_ID="5"
AZURE_OPENAI_API_VERSION="2024-02-01"

# 🟢 Optional (Has Defaults)
AI_MODEL="gpt-4o-mini"
EMBEDDING_MODEL="text-embedding-3-large"
EMBED_WORKSHEETS="true"
DB_HOST="postgres"
DB_PORT="5432"
```

### Test Environment

```bash
# 🔴 Critical (Must Set) - Different from dev
AZURE_OPENAI_ENDPOINT="https://d837ad-test-econ-llm-east.openai.azure.com/"
AZURE_OPENAI_API_KEY="test-api-key-here"
AZURE_OPENAI_DEPLOYMENT="gpt-4o-mini"
AZURE_OPENAI_EMBEDDING_DEPLOYMENT="text-embedding-3-large"
JWT_SECRET="test-jwt-secret-64-chars-minimum"
METABASE_KEY="test-metabase-api-key"
MB_EMBED_SECRET="test-metabase-embed-secret"
MB_URL="https://test-unity-reporting.apps.silver.devops.gov.bc.ca"
MB_EMBED_ID="3"
POSTGRES_PASSWORD="test-postgres-password"

# 🟡 Environment-Specific
FLASK_ENV="test"
POSTGRES_DB="unity_ai_test"
POSTGRES_USER="test_user"
DEFAULT_EMBED_DB_ID="3"
```

### UAT Environment

```bash
# 🔴 Critical (Must Set) - Different from dev/test
AZURE_OPENAI_ENDPOINT="https://d837ad-uat-econ-llm-east.openai.azure.com/"
AZURE_OPENAI_API_KEY="uat-api-key-here"
AZURE_OPENAI_DEPLOYMENT="gpt-4o-mini"
AZURE_OPENAI_EMBEDDING_DEPLOYMENT="text-embedding-3-large"
JWT_SECRET="uat-jwt-secret-64-chars-minimum"
METABASE_KEY="uat-metabase-api-key"
MB_EMBED_SECRET="uat-metabase-embed-secret"
MB_URL="https://uat-unity-reporting.apps.silver.devops.gov.bc.ca"
MB_EMBED_ID="5"
POSTGRES_PASSWORD="uat-postgres-password"

# 🟡 Environment-Specific
FLASK_ENV="staging"
POSTGRES_DB="unity_ai_uat"
POSTGRES_USER="uat_user"
DEFAULT_EMBED_DB_ID="5"
```

### Production Environment

```bash
# 🔴 Critical (Must Set) - Different from dev/test/uat
AZURE_OPENAI_ENDPOINT="https://d837ad-prod-econ-llm-east.openai.azure.com/"
AZURE_OPENAI_API_KEY="prod-api-key-here"
AZURE_OPENAI_DEPLOYMENT="gpt-4-32k"
AZURE_OPENAI_EMBEDDING_DEPLOYMENT="text-embedding-3-large"
JWT_SECRET="prod-jwt-secret-64-chars-minimum"
METABASE_KEY="prod-metabase-api-key"
MB_EMBED_SECRET="prod-metabase-embed-secret"
MB_URL="https://unity-reporting.apps.silver.devops.gov.bc.ca"
MB_EMBED_ID="3"
POSTGRES_PASSWORD="prod-postgres-password"

# 🟡 Environment-Specific
FLASK_ENV="production"
POSTGRES_DB="unity_ai_prod"
POSTGRES_USER="prod_user"
DEFAULT_EMBED_DB_ID="3"
AI_MODEL="gpt-4-32k"
```

## GitOps Implementation Examples

### ConfigMap Example (From GitOps Overlays)

```yaml
# From overlays/dev/patches/environment-patches.yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: unity-ai-reporting
data:
  FLASK_ENV: "development"
  DEFAULT_EMBED_DB_ID: "5"
  MB_URL: "https://dev-unity-reporting.apps.silver.devops.gov.bc.ca"
```

### Secret Example (Critical Variables)

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: unity-ai-secrets
  namespace: d18498-dev
type: Opaque
stringData:
  # Critical secrets (no defaults)
  AZURE_OPENAI_ENDPOINT: "https://d837ad-dev-econ-llm-east.openai.azure.com/"
  AZURE_OPENAI_API_KEY: "your-dev-api-key-here"
  JWT_SECRET: "your-64-char-jwt-secret-here"
  METABASE_KEY: "your-dev-metabase-key-here"
  MB_EMBED_SECRET: "your-dev-embed-secret-here"
  POSTGRES_PASSWORD: "your-dev-db-password"
```

### StatefulSet Environment Configuration (From GitOps)

```yaml
# From overlays/*/patches/environment-patches.yaml
apiVersion: apps/v1
kind: StatefulSet
metadata:
  name: unity-ai-reporting
spec:
  replicas: 1  # dev=1, test=3, uat=3, prod=varies
  template:
    spec:
      containers:
      - name: unity-ai-reporting
        image: image-registry.openshift-image-registry.svc:5000/d18498-dev/dev-unity-ai-reporting:latest
        env:
        - name: FLASK_ENV
          valueFrom:
            configMapKeyRef:
              name: dev-unity-ai-reporting
              key: FLASK_ENV
        - name: AZURE_OPENAI_ENDPOINT
          valueFrom:
            secretKeyRef:
              name: dev-unity-ai-secrets
              key: AZURE_OPENAI_ENDPOINT
        - name: DEFAULT_EMBED_DB_ID
          valueFrom:
            configMapKeyRef:
              name: dev-unity-ai-reporting
              key: DEFAULT_EMBED_DB_ID
        - name: MB_EMBED_ID
          valueFrom:
            configMapKeyRef:
              name: dev-unity-ai-reporting
              key: DEFAULT_EMBED_DB_ID  # Note: MB_EMBED_ID uses same value as DEFAULT_EMBED_DB_ID
        # ... (all other environment variables follow this pattern)
```

## Key Insights from GitOps Configuration

### Important Notes:

1. **MB_EMBED_ID Configuration**: In the actual GitOps setup, `MB_EMBED_ID` is set to the same value as `DEFAULT_EMBED_DB_ID`
2. **Environment-Specific Replicas**: 
   - Dev: 1 replica
   - Test: 3 replicas  
   - UAT: 3 replicas
   - Prod: varies
3. **Namespace Sharing**: UAT shares the d18498-test namespace with Test environment
4. **Image Tagging**: Different environments use different image tags (latest vs stable)

## Configuration Validation

### Test Database Connectivity
After configuring, verify the backend can connect:

```bash
# Check application logs
oc logs statefulset/dev-unity-ai-reporting -n d18498-dev

# Look for successful Metabase API calls
# Should see successful responses from /api/database/{MB_EMBED_ID}/metadata
```

### How to Determine Database IDs

1. **Check Metabase Admin Interface**:
   - Log into your environment's Metabase instance
   - Go to Settings → Admin → Databases
   - Note the database ID number in the URL or database list

2. **API Query**:
   ```bash
   curl -H "x-api-key: YOUR_METABASE_KEY" \
     https://your-metabase-url/api/database
   ```

## Common Issues

### Issue: Application fails with "database not found"
**Solution**: Verify `MB_EMBED_ID` matches an actual database in your Metabase instance

### Issue: Tenant mapping uses wrong database
**Solution**: Ensure `DEFAULT_EMBED_DB_ID` is set correctly for your environment

### Issue: API calls fail with authentication errors
**Solution**: Verify `METABASE_KEY` has access to the specified database ID

### Issue: JWT authentication fails
**Solution**: Ensure `JWT_SECRET` is set and is at least 64 characters long

## Security Best Practices

1. **Never commit secrets to Git** - Use External Secrets Operator or manual secret creation
2. **Environment isolation** - Each environment should have completely separate credentials
3. **Secret rotation** - Regularly rotate JWT secrets, API keys, and database passwords (use `openssl rand -base64 64` for JWT secrets)
4. **Least privilege** - Database users should only have required permissions
5. **Network policies** - Restrict pod-to-pod communication where possible
6. **Environment-specific API keys** - Use different Azure OpenAI keys per environment to prevent cross-environment data leaks
7. **Validate critical variables** - Application will fail to start if critical variables (marked 🔴) are missing
8. **Build-time vs Runtime** - Build arguments are baked into images, runtime environment variables are provided during deployment

## Local Development

For local development using docker-compose, the `.env` file contains default values that will be overridden by environment-specific values in OpenShift deployments.