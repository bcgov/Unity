# Core — Integrations

Unity talks to seven external systems from the core, plus two more owned by modules. This document covers the core's own clients and the shared plumbing they run on: dynamic URLs and resilient HTTP.

| System | What for | Client |
|---|---|---|
| **CHEFS** | Form definitions, submissions, attachments | `Integrations/Chefs/FormsApiService` |
| **CSS** | Keycloak user search and role management | `Integrations/Css/CssApiService` |
| **OrgBook** | BC organisation registry lookup | `Integrations/OrgBook/OrgBookService` |
| **Geocoder** | BC address and location resolution | `Integrations/Geocoder/GeocoderApiService` |
| **Metabase** | Embedded reporting | `Integrations/Metabase/MetabaseApiClient` |
| **Matomo** | Web analytics | `MatomoUrlProvider` |
| **GitHub** | Blame lookup on exception alerts | `Web/Middleware/GitHubBlameLookupService` |
| **CAS** | Payments — supplier and invoice | Owned by [`payments/`](../payments/payments-cas-integration.md) |
| **CHES** | Email — owned by [`notifications/`](../notifications/notifications-email-pipeline.md) |

## Dynamic URLs

No external base URL is a constant or a configuration key. Every one is a **row in the host database**, resolved at runtime through `IEndpointManagementAppService`.

`DynamicUrlKeyNames` (`Domain.Shared/Integrations/`) is the key list:

| Key | Points at |
|---|---|
| `INTAKE_API_BASE` | CHEFS |
| `CSS_API_BASE`, `CSS_TOKEN_API_BASE` | CSS API and its token endpoint |
| `PAYMENT_API_BASE` | CAS |
| `NOTIFICATION_API_BASE`, `NOTIFICATION_AUTH` | CHES and its auth endpoint |
| `ORGBOOK_API_BASE` | OrgBook |
| `GEOCODER_API_BASE`, `GEOCODER_LOCATION_API_BASE` | Geocoder |
| `METABASE_API_BASE`, `REPORTING_AI` | Reporting hosts |
| `ANALYTICS_MATOMO_BASE` | Matomo |
| `GITHUB_REPO`, `GITHUB_GRAPHQL` | GitHub |
| `DIRECT_MESSAGE_` *(prefix)* | Teams direct-message webhooks, numerically incremented |
| `WEBHOOK_` *(prefix)* | General webhooks, numerically incremented |

The two prefixes are the exception to one-key-one-row: multiple rows share a prefix with an incrementing suffix, so an operator can add another Teams channel without a code change.

`EndpointManagementAppService` (211 lines) resolves them, with typed helpers such as `GetChefsApiBaseUrlAsync()` alongside the generic `GetUgmUrlByKeyNameAsync(keyName)`. A missing row raises `UserFriendlyException`, which is why several callers wrap the lookup and degrade rather than fail — the AI Reporting page renders empty, `SupplierService` throws only when actually used.

`DynamicUrlDataSeeder` seeds the defaults, each row carrying a description (`"BC Corporate Accounting Services API"`).

**Repointing an integration is a data change, not a deployment.** That is the design intent, and it is why environment-specific URLs do not appear in `appsettings`.

## Resilient HTTP

`IResilientHttpRequest` (`Unity.Modules.Shared.Http`) is the shared client every integration goes through:

```csharp
await resilientHttpRequest.HttpAsync(HttpMethod.Get, resource, body: null, authToken);
```

It centralises retry and timeout policy, and `ResilientHttpRequest.ContentToStringAsync(response.Content)` is the conventional way to read a body. `IntegrationServiceException` (`Integrations/Exceptions/`) is the shared failure type.

Integration services are marked `[IntegrationService]` and usually `[RemoteService(false)]`, so they are injectable but not exposed as HTTP endpoints.

## CHEFS

`FormsApiService` is the core's CHEFS client. Authentication is **per form**: the `ApplicationForm.ApiKey`, stored encrypted, used as basic auth with the CHEFS form GUID as the username. There is no tenant-wide CHEFS credential.

| Operation | Used by |
|---|---|
| `GetSubmissionDataAsync(formId, submissionId)` | Intake, attachment resync |
| Form and version metadata | `ApplicationFormVersionAppService` initialisation |
| Submission listing | `ApplicationFormSycnronizationService.GetSubmissionsList` |

Two behaviours worth knowing: requests are **paced** at 500 ms (`ChefsRequestPacingDelay`) when sweeping many forms, and the webhook payload is never trusted — Unity re-fetches the submission itself. See [core-intake.md](core-intake.md) and [core-forms-and-versions.md](core-forms-and-versions.md#synchronising-with-chefs).

## CSS

`CssApiService` (221 lines) is the Keycloak-side integration — searching users and managing their role assignments in the CSS (Common Hosted Single Sign-On) service.

Its token handling is the part to know: the service caches its access token and refreshes when the current one **expires within the next five minutes**, rather than on failure. `CssApiOptions` carries the client configuration.

`CssOnboardingUserLookup` is a related lookup used by tenant onboarding — see [`tenant-management/tenant-management-onboarding.md`](../tenant-management/tenant-management-onboarding.md).

## OrgBook

`OrgBookService` queries BC's OrgBook registry to resolve an organisation's registration details from a name or number. It feeds applicant organisation data — `OrgName`, `OrgNumber`, `OrgStatus` — and supports the duplicate-detection matching described in [core-applicants.md](core-applicants.md#duplicate-detection).

## Geocoder

`GeocoderApiService` resolves BC addresses and locations, with `ResultMapper` translating the response into Unity's shape. Two endpoints are configured — `GEOCODER_API_BASE` for address lookup and `GEOCODER_LOCATION_API_BASE` for location.

It is what backs address autocomplete on applicant addresses, and contributes to deriving the electoral district, regional district and economic region recorded on an application.

## Metabase

`MetabaseApiClient` (288 lines, behind `IMetabaseApiClient`, configured by `MetabaseOptions`) drives the embedded reporting experience. Metabase reads the tenant database through the **read-only** connection string provisioned by the schema migrator — see [core-persistence.md](core-persistence.md#provisioning-a-tenant-database) — and the reporting views it reads are documented in [`reporting/`](../reporting/README.md).

`REPORTING_AI` is a separate dynamic URL for the AI reporting host, embedded by the AI module's reporting page.

## Matomo

`MatomoUrlProvider` supplies the Matomo tracking URL from `ANALYTICS_MATOMO_BASE`, and returns nothing unless the `Unity.Analytics` tenant feature is enabled. The type exists twice — in `Application/Analytics/` and `Web/Analytics/` — with the web copy being the one the layout uses.

## GitHub

The odd one out: `GitHubBlameLookupService` (behind `IBlameLookupService`) queries the GitHub GraphQL API to find who last touched the line an exception came from, so an exception alert can name a likely owner. Configured through `GITHUB_REPO` and `GITHUB_GRAPHQL`. See [core-background-and-ops.md](core-background-and-ops.md#exception-notifications).

## Teams webhooks

Teams notifications go to webhook URLs stored under the `DIRECT_MESSAGE_` and `WEBHOOK_` dynamic-URL prefixes. Two core paths post to them — `NotifyChefsEventToTeamsAsync` for rejected CHEFS submissions during intake validation, and the exception-notification subscriber.

Note that the Notifications module's own Teams integration is **disabled but not removed** — see [`notifications/notifications-roadmap.md`](../notifications/notifications-roadmap.md). The core paths described here are separate and do post.

## Inbound integrations

Traffic arriving *at* Unity, on `HttpApi` controllers:

| Controller | Route | Auth |
|---|---|---|
| `EventSubscriptionController` | `api/chefs/event`, `api/chefs/event/{__tenant}` | `[AllowAnonymous]` — CHEFS webhook |
| `FormController` | `api/app/form/{formId}/version/{formVersionId}` | Synchronise available fields |
| `ApplicantProfileController`, `ApplicantLookupController` | Applicant portal | See [`applicant-portal/`](../applicant-portal/applicant-portal-integration.md) |
| `AttachmentController` | 769 lines — attachment upload and download | |
| `ConfigurationFileController` | Configuration file serving | |
| `Authentication/` | Token endpoints | |

The RabbitMQ side — the Grants Portal command consumer and the inbox/outbox workers — is documented in [`applicant-portal/grants-portal-rabbitmq-integration.md`](../applicant-portal/grants-portal-rabbitmq-integration.md) and [`transactional-outbox-pattern.md`](../transactional-outbox-pattern.md).
