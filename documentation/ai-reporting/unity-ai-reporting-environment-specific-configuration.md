# Environment-Specific Configuration Guide

## Overview

The Unity AI platform uses two sources of runtime configuration:

1. **Environment variables** — injected via OpenShift Secrets and ConfigMaps (or `.env` locally)
2. **Tenant config** — a JSON file (`tenant_config.json`) that maps tenants to Metabase databases and schema settings, overridden per environment via the `[env]-unity-ai-tenant-config` OpenShift Secret

Non-sensitive config (deployment names, model versions, database port) is hardcoded in `config.py` and does not require environment variables.

---

## Environment Variables Reference

### 🔴 Secrets (OpenShift Secret — no defaults, must be set)

| Variable | Purpose | Example |
|----------|---------|---------|
| `AZURE_OPENAI_ENDPOINT` | Azure OpenAI service endpoint | `https://d837ad-dev-econ-llm-east.openai.azure.com/` |
| `AZURE_OPENAI_API_KEY` | Azure OpenAI API key | Environment-specific key |
| `JWT_SECRET` | JWT signing secret (64+ chars) | `openssl rand -base64 64` |
| `DB_PASSWORD` | PostgreSQL password | Environment-specific password |

### 🟡 ConfigMap (environment-specific, non-sensitive)

| Variable | Purpose | Example Values |
|----------|---------|----------------|
| `MB_URL` | Metabase instance base URL | `https://dev-unity-reporting.apps.silver.devops.gov.bc.ca` |
| `MB_MAP_REGION_UUID` | Metabase UUID for the Regional Districts boundary layer | Differs per Metabase instance |
| `ORIGIN_URL` | Comma-separated allowed iframe parent origins | `https://dev.example.com` |
| `FLASK_ENV` | Flask environment mode | `development`, `production` |
| `DB_HOST` | PostgreSQL host | `[env]-unity-ai-postgres` |
| `DB_NAME` | PostgreSQL database name | `unity_ai` |
| `DB_USER` | PostgreSQL username | `unity_user` |

### 🔧 Build-time (baked into Docker image)

| Variable | Purpose | Default |
|----------|---------|---------|
| `UAI_BUILD_VERSION` | Application version | `0.0.0` |
| `UAI_BUILD_REVISION` | Git commit hash | `0000000` |
| `UAI_TARGET_ENVIRONMENT` | Target environment label | `Development` |

### Hardcoded in `config.py` (no env var needed)

| Setting | Value |
|---------|-------|
| Azure OpenAI deployment | `gpt-5-mini` |
| Azure OpenAI embedding deployment | `text-embedding-3-large` |
| Azure OpenAI API version | `2024-10-21` |
| Database port | `5432` |

---

## Tenant Configuration

The Metabase API key and per-tenant database mappings are **not** environment variables. They live in a JSON file that is mounted into the container as a secret.

### Structure

```json
{
  "Default Grants Program": {
    "db_id": 5,
    "collection_id": 16,
    "schema_types": ["public", "custom"],
    "api_key": "mb_your_metabase_api_key_here"
  },
  "REDIP": {
    "db_id": 9,
    "collection_id": 93,
    "schema_types": ["public", "custom"],
    "api_key": "mb_your_metabase_api_key_here"
  }
}
```

### OpenShift

Each environment has an `[env]-unity-ai-tenant-config` Secret whose value is mounted over the committed `tenant_config.json` at `/app/backend/src/tenant_config.json`. This is where `api_key` and environment-specific `db_id`/`collection_id` values are set.

### Local development

Create `applications/Unity.AI.Reporting.Backend/src/tenant_config.local.json` (gitignored). The app merges it over `tenant_config.json` at startup — only include the fields you want to override:

```json
{
  "Default Grants Program": { "api_key": "mb_your_local_key_here" },
  "REDIP": { "api_key": "mb_your_local_key_here" }
}
```

For Docker Compose, copy `docker-compose.override.yml.example` to `docker-compose.override.yml` (also gitignored) to mount the file into the container.

---

## OpenShift ConfigMap / Secret Examples

### Secret

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: dev-unity-ai-secrets
  namespace: d18498-dev
type: Opaque
stringData:
  AZURE_OPENAI_ENDPOINT: "https://d837ad-dev-econ-llm-east.openai.azure.com/"
  AZURE_OPENAI_API_KEY: "your-dev-api-key"
  JWT_SECRET: "your-64-char-jwt-secret"
  DB_PASSWORD: "your-dev-db-password"
```

### Tenant Config Secret

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: dev-unity-ai-tenant-config
  namespace: d18498-dev
type: Opaque
stringData:
  tenant_config.json: |
    {
      "Default Grants Program": {
        "db_id": 5,
        "collection_id": 16,
        "schema_types": ["public", "custom"],
        "api_key": "mb_your_dev_metabase_api_key"
      },
      "REDIP": {
        "db_id": 9,
        "collection_id": 93,
        "schema_types": ["public", "custom"],
        "api_key": "mb_your_dev_metabase_api_key"
      }
    }
```

### ConfigMap

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: dev-unity-ai-reporting
  namespace: d18498-dev
data:
  FLASK_ENV: "development"
  MB_URL: "https://dev-unity-reporting.apps.silver.devops.gov.bc.ca"
  MB_MAP_REGION_UUID: "your-regional-districts-uuid"
  ORIGIN_URL: "https://dev-grants.example.com"
  DB_HOST: "dev-unity-ai-postgres"
  DB_NAME: "unity_ai"
  DB_USER: "unity_user"
```

---

## GitOps Repository Structure

```
tenant-gitops-d18498/manifests/uai/overlays/
├── dev/     # d18498-dev namespace
├── test/    # d18498-test namespace
├── uat/     # d18498-test namespace (shares test namespace)
└── prod/    # d18498-prod namespace
```

---

## Determining `db_id` Values

1. Log into the environment's Metabase instance
2. Go to Settings → Admin → Databases
3. Note the database ID from the URL or database list

Or via API (using the `api_key` from tenant config):

```bash
curl -H "x-api-key: YOUR_METABASE_API_KEY" \
  https://your-metabase-url/api/database
```

---

## Common Issues

### API calls fail with authentication errors
Verify the `api_key` in the tenant config secret has access to the Metabase instance.

### Application fails with "database not found"
Verify `db_id` in the tenant config matches an actual database in the environment's Metabase instance.

### JWT authentication fails
Ensure `JWT_SECRET` is set and is at least 64 characters long.

### Map visualization shows wrong region boundaries
Check `MB_MAP_REGION_UUID` — it must match the Regional Districts layer UUID in that Metabase instance.

---

## Security Best Practices

1. Never commit secrets to Git — use the `[env]-unity-ai-tenant-config` Secret and `[env]-unity-ai-secrets` for all sensitive values
2. Each environment must have completely separate credentials
3. Rotate JWT secrets, API keys, and database passwords regularly (`openssl rand -base64 64` for JWT)
4. Database users should have only the required permissions
5. Use different Azure OpenAI keys per environment to prevent cross-environment data leaks
