# Environment-Specific Configuration Guide

## Overview

The Unity AI platform's runtime configuration is **environment variables**, injected via OpenShift Secrets and ConfigMaps (or `.env` locally). Each program's Metabase database and collection come from the UGM tenant record (Reporting tab) in the AI Reporting token (AB#33978); there is no `tenant_config.json` and no `[env]-unity-ai-tenant-config` Secret any more.

The Azure OpenAI models AI Reporting uses (chat and embedding deployment, and each one's API version) are **not** configuration here: they are set in Unity Admin → AI Setup → AI Reporting and arrive in the AI Reporting token from UGM (AB#35030). AI Reporting refuses a request without them.

---

## Environment Variables Reference

### 🔴 Secrets (OpenShift Secret — no defaults, must be set)

| Variable | Purpose | Example |
|----------|---------|---------|
| `AZURE_OPENAI_ENDPOINT` | Azure OpenAI service endpoint | `https://your-azure-openai-resource.openai.azure.com/` |
| `AZURE_OPENAI_API_KEY` | Azure OpenAI API key | Environment-specific key |
| `JWT_SECRET` | JWT signing secret (64+ chars) | `openssl rand -base64 64` |
| `DB_PASSWORD` | PostgreSQL password | Environment-specific password |
| `METABASE_API_KEY` | Metabase API key, shared by every program | From the existing `unity-metabase` Secret (`MB_UNITY_ADMIN_API_KEY`) |

### 🟡 ConfigMap (environment-specific, non-sensitive)

| Variable | Purpose | Example Values |
|----------|---------|----------------|
| `MB_URL` | Metabase instance base URL | `https://your-metabase-instance.example.com` |
| `MB_MAP_REGION_UUID` | Metabase UUID for the Regional Districts boundary layer | Differs per Metabase instance |
| `ORIGIN_URL` | Comma-separated allowed iframe parent origins | `https://dev.example.com` |
| `FLASK_ENV` | Flask environment mode | `development`, `production` |
| `DB_HOST` | PostgreSQL host | `[env]-unity-ai-postgres` |
| `DB_NAME` | PostgreSQL database name | `UnityAI` |
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
| Database port | `5432` |

### Set in Unity AI Setup (sent in the token)

| Token claim | AI Setup |
|---|---|
| `ai_chat_deployment`, `ai_chat_api_version` | AI Reporting → Chat model, and that model's API version |
| `ai_embedding_deployment`, `ai_embedding_api_version` | AI Reporting → Embedding model, and that model's API version |

UAI stores them per program in `tenant_registry` (with `embedded_with`, the embedding deployment the schema was embedded with). When the embedding model changes, the next request (or `app.py embed` / `embed-all`) re-embeds that program's schema and clears its query cache. Vectors from before AB#35030 have no `embedded_with`; they count as `text-embedding-3-large`, the model UAI then had in code. The semantic cache stores 3,072-dimension vectors (`halfvec(3072)`), so pick a 3,072-dimension embedding model such as `text-embedding-3-large`; with another size, reports still work but the cache is skipped and an error is logged.

---

## Program (tenant) settings

UAI keeps one row per program in its `tenant_registry` table, written from the AI Reporting token the first time the program opens AI Reporting and refreshed on every later token: Metabase `db_id` (`mb_db_id`), `collection_id` (`mb_collection_id`), schema types, and the AI Setup models above. Set the Metabase ids on the program's Reporting tab in UGM; nothing is configured in UAI.

---

## OpenShift ConfigMap / Secret Examples

### Secret

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: dev-unity-ai-secrets
  namespace: <namespace>-dev
type: Opaque
stringData:
  AZURE_OPENAI_ENDPOINT: "https://your-azure-openai-resource.openai.azure.com/"
  AZURE_OPENAI_API_KEY: "your-dev-api-key"
  JWT_SECRET: "your-64-char-jwt-secret"
  DB_PASSWORD: "your-dev-db-password"
```

### ConfigMap

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: dev-unity-ai-reporting
  namespace: <namespace>-dev
data:
  FLASK_ENV: "development"
  MB_URL: "https://your-metabase-instance.example.com"
  MB_MAP_REGION_UUID: "your-regional-districts-uuid"
  ORIGIN_URL: "https://dev-grants.example.com"
  DB_HOST: "dev-unity-ai-postgres"
  DB_NAME: "UnityAI"
  DB_USER: "unity_user"
```

---

## GitOps Repository Structure

```
tenant-gitops-<namespace>/manifests/uai/overlays/
├── dev/     # <namespace>-dev namespace
├── test/    # <namespace>-test namespace
├── uat/     # <namespace>-test namespace (shares test namespace)
└── prod/    # <namespace>-prod namespace
```

---

## Determining `db_id` Values

1. Log into the environment's Metabase instance
2. Go to Settings → Admin → Databases
3. Note the database ID from the URL or database list

Or via API (using `METABASE_API_KEY`):

```bash
curl -H "x-api-key: YOUR_METABASE_API_KEY" \
  https://your-metabase-url/api/database
```

---

## Common Issues

### API calls fail with authentication errors
Verify `METABASE_API_KEY` has access to the Metabase instance.

### Application fails with "database not found"
Verify the program's Metabase database id on its UGM Reporting tab matches an actual database in the environment's Metabase instance.

### JWT authentication fails
Ensure `JWT_SECRET` is set and is at least 64 characters long.

### Map visualization shows wrong region boundaries
Check `MB_MAP_REGION_UUID` — it must match the Regional Districts layer UUID in that Metabase instance.

---

## Security Best Practices

1. Never commit secrets to Git — use `[env]-unity-ai-secrets`, `[env]-unity-ai-postgres` and `unity-metabase` for all sensitive values
2. Each environment must have completely separate credentials
3. Rotate JWT secrets, API keys, and database passwords regularly (`openssl rand -base64 64` for JWT)
4. Database users should have only the required permissions
5. Use different Azure OpenAI keys per environment to prevent cross-environment data leaks
