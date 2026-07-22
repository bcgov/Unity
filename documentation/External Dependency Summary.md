# Unity Platform External Dependency Summary

## Unity Grant Manager (UGM): 15 External Dependencies

| # | Service | Key | URL | Purpose |
|---|---|---|---|---|
| 1 | Keycloak / LoginProxy | AuthServer__ServerAddress | loginproxy.gov.bc.ca/auth | OIDC user login |
| 2 | CSS API | CSS_API_BASE | api.loginproxy.gov.bc.ca/api/v1 | IDIR staff directory lookup |
| 3 | CSS Token | CSS_TOKEN_API_BASE | .../realms/standard/.../token | OAuth token for CSS |
| 4 | CHES | NOTIFICATION_API_BASE | ches.api.gov.bc.ca/api/v1 | Send/status/cancel emails |
| 5 | CHES Auth | NOTIFICATION_AUTH | .../realms/comsvcauth/.../token | OAuth token for CHES |
| 6 | Teams Webhooks | DIRECT_MESSAGE_0...N | bcgov.webhook.office.com/... | Direct Teams channel messages |
| 7 | CAS / CFS | PAYMENT_API_BASE | cfs-prodws.cas.gov.bc.ca:7121/ords/cas | Supplier lookup + invoice creation |
| 8 | BC Geocoder | GEOCODER_LOCATION_API_BASE | geocoder.api.gov.bc.ca | Address/region lookups |
| 9 | OpenMaps WFS | GEOCODER_API_BASE | openmaps.gov.bc.ca/geo/pub/ows?... | Geospatial boundary features |
| 10 | OrgBook | ORGBOOK_API_BASE | orgbook.gov.bc.ca/api | Business registry search |
| 11 | CHEFS | INTAKE_API_BASE | submit.digital.gov.bc.ca/app/api/v1 | Form definitions + submissions |
| 12 | Azure OpenAI | OpenAI:Endpoint + OpenAI:ApiKey (Vault) | Azure-hosted (per env) | AI: AttachmentSummary, ApplicationAnalysis, ApplicationScoring |
| 13 | Reporting AI | REPORTING_AI | prod-unity-ai-reporting-d18498-prod.apps.silver.devops.gov.bc.ca | iFrame embed only — no backend HTTP call |
| 14 | Matomo Analytics | ANALYTICS_MATOMO_BASE | {env}-analytics-matomo.apps.silver.devops.gov.bc.ca | Browser-side page tracking |
| 15 | HashiCorp Vault | (platform / ExternalSecrets) | vault.developer.gov.bc.ca | All API keys, OAuth secrets, DB credentials |

## Unity Applicant Portal (UAP): 4 External Dependencies

| # | Service | Config Key | URL | Purpose |
|---|---|---|---|---|
| 1 | Keycloak / LoginProxy | KEYCLOAK__AUTHSERVERURL | loginproxy.gov.bc.ca/auth | OIDC token exchange + userinfo |
| 2 | OrgBook | orgbookApiUrl | orgbook.gov.bc.ca/api | Business registry - direct browser calls |
| 3 | Matomo Analytics | MATOMO__URL | {env}-analytics-matomo.apps.silver.devops.gov.bc.ca/ | Browser-side page tracking |
| 4 | Unity Grant Manager API | Plugins:UNITY:Configuration:BaseUrl | http://{env}-unity-grantmanager-web (in-cluster) | Profile/tenant reads (HTTP) + Contact/Address mutations (RabbitMQ) |

---

## Key Findings

- Azure OpenAI is UGM's operational AI endpoint — makes live chat completion calls (analysis, scoring, attachment summaries). Endpoint URL and key come from Vault.
- OrgBook is called by both apps: UGM from its backend, UAP directly from the Angular browser.
- UAP to UGM use sync REST (profile reads via plugin HTTP client) and async RabbitMQ (contact/address mutations).
- All secrets live in HashiCorp Vault and are pulled into OCP via the ExternalSecrets operator.
