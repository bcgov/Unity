# Unity Portal Documentation Index

This is a **source-path → documentation** map. Its purpose is mechanical: before opening a PR, look up the paths your change touched and confirm the listed docs are still true.

All paths below are relative to the repository root. `applications/Unity.GrantManager/` is abbreviated as **`AGM/`** throughout.

## How to use this index

1. List the paths your change touched (`git diff --name-only <base>...HEAD`).
2. Find each path's row below. If no row matches, no doc covers that area — see [Undocumented areas](#undocumented-areas).
3. Open the listed docs and check whether your change made anything in them wrong: a renamed class, a changed state machine, a new step in a documented sequence, a removed endpoint.
4. Update what's now inaccurate. Do **not** create new documentation for an undocumented area unless someone asked for it.

The bar is *"is anything here now false?"* — not *"should I describe what I did?"*. Most changes require no doc edit at all.

## Path map

| Source path | Docs to check | Covers |
|---|---|---|
| `AGM/src/Unity.GrantManager.Application/Intakes/**`<br>`AGM/src/Unity.GrantManager.Application/ApplicationForms/**`<br>`AGM/src/Unity.GrantManager.HttpApi/Controllers/EventSubscriptionController.cs`<br>`AGM/src/Unity.GrantManager.HttpApi/Controllers/FormController.cs` | [`core/core-intake.md`](core/core-intake.md)<br>[`core/core-forms-and-versions.md`](core/core-forms-and-versions.md) | The CHEFS webhook, submission validation and mapping, application creation, Unity application IDs, the nightly resync, form and form-version configuration |
| `AGM/src/Unity.GrantManager.Domain/Applications/Application*.cs`<br>`AGM/src/Unity.GrantManager.Domain.Shared/GrantApplications/**`<br>`AGM/src/Unity.GrantManager.Application/GrantApplications/**` (excl. `Automation/`) | [`core/core-application-lifecycle.md`](core/core-application-lifecycle.md) | The `Application` aggregate, the hierarchical 15-state workflow, direct approval, assignments, owners, links, field-update permission tiers |
| `AGM/src/Unity.GrantManager.Domain/Assessments/**`<br>`AGM/src/Unity.GrantManager.Domain.Shared/Assessments/**`<br>`AGM/src/Unity.GrantManager.Application/Assessments/**` | [`core/core-assessment.md`](core/core-assessment.md) | The `Assessment` aggregate and its workflow, creation rules, scoresheet instances, AI assessments and cloning, resource-based authorization |
| `AGM/src/Unity.GrantManager.Domain/Applications/Applicant*.cs`<br>`AGM/src/Unity.GrantManager.Application/Applicants/**`<br>`AGM/src/Unity.GrantManager.Domain/Contacts/**` | [`core/core-applicants.md`](core/core-applicants.md) | The `Applicant` aggregate, applicant agents, addresses and the primary-per-type rule, contacts, duplicate detection, merge/unmerge |
| `AGM/src/Unity.GrantManager.Application/Attachments/**`<br>`AGM/src/Unity.GrantManager.HttpApi/Controllers/AttachmentController.cs`<br>`AGM/src/Unity.GrantManager.Domain.Shared/Attachments/**` | [`core/core-attachments.md`](core/core-attachments.md) | The CHEFS-held and S3 storage paths, the attachment entity hierarchy, preview and LibreOffice PDF conversion |
| `AGM/src/Unity.GrantManager.Domain/Comments/**`<br>`AGM/src/Unity.GrantManager.Domain/Tags/**`<br>`AGM/src/Unity.GrantManager.Application/Tags/**` | [`core/core-comments-and-tags.md`](core/core-comments-and-tags.md) | Comment threads on applications, assessments and applicants; the tag table shared with Payments and the two-event deletion protocol |
| `AGM/src/Unity.GrantManager.Application/History/**`<br>`AGM/src/Unity.GrantManager.Domain/Applications/AuditHistory.cs`<br>`AGM/src/Unity.GrantManager.Domain/Applications/FundingHistory.cs`<br>`AGM/src/Unity.GrantManager.EntityFrameworkCore/Repositories/EfCoreAuditLogRepository.cs` | [`core/core-history-and-audit.md`](core/core-history-and-audit.md) | Change history read from ABP's audit log, the manually maintained funding and audit registers, and the payment-history type filter |
| `AGM/src/Unity.GrantManager.Domain.Shared/Workflow/**` | [`core/core-overview.md`](core/core-overview.md) | Project layering, multi-tenancy, module composition, the shared `UnityWorkflow` |
| `AGM/src/Unity.GrantManager.EntityFrameworkCore/**` | [`core/core-persistence.md`](core/core-persistence.md) | The two DbContexts, the host/tenant migration split, per-tenant database provisioning and role grants, flattened migrations, repositories, startup warm-up |
| `AGM/modules/Unity.SharedKernel/Constants/UnitySelector*.cs`<br>`AGM/src/Unity.GrantManager.Domain/Permissions/**`<br>`AGM/src/Unity.GrantManager.Application/Identity/**`<br>`AGM/src/Unity.GrantManager.Application/Integrations/Css/**` | [`core/core-permissions-and-identity.md`](core/core-permissions-and-identity.md) | The permission tree and override permissions, roles and permission seeding, Keycloak/CSS, the five authorization mechanisms |
| `AGM/src/Unity.GrantManager.Domain/Zones/**`<br>`AGM/src/Unity.GrantManager.Web/TagHelpers/Zone/**`<br>`AGM/src/Unity.GrantManager.Web/Pages/FormConfiguration/**` | [`core/core-zones-and-ui-config.md`](core/core-zones-and-ui-config.md) | Zone definitions and the default template, setting-backed per-form and per-tenant configuration, the `<zone>` tag helper |
| `AGM/src/Unity.GrantManager.Application/Integrations/**`<br>`AGM/src/Unity.GrantManager.Domain.Shared/Integrations/DynamicUrlKeyNames.cs` | [`core/core-integrations.md`](core/core-integrations.md) | Dynamic URLs, resilient HTTP, and the CHEFS, CSS, OrgBook, Geocoder, Metabase, Matomo and GitHub clients |
| `AGM/src/Unity.GrantManager.Web/GrantManagerWebModule.cs`<br>`AGM/src/Unity.GrantManager.Web/Middleware/**`<br>`AGM/src/Unity.GrantManager.Web/Menus/**`<br>`AGM/src/Unity.GrantManager.Web/Views/Shared/Components/**` | [`core/core-web-shell.md`](core/core-web-shell.md) | Module composition, the request pipeline, custom middleware, menus, shared view components, theming and bundles |
| `AGM/src/Unity.GrantManager.Application/HealthChecks/**`<br>`AGM/src/Unity.GrantManager.Application/Locks/**`<br>`AGM/src/Unity.GrantManager.Application/Logs/**`<br>`AGM/src/Unity.GrantManager.Application/Dashboard/**` | [`core/core-background-and-ops.md`](core/core-background-and-ops.md) | Quartz workers and the per-tenant sweep, distributed locking, health checks, exception logging and Teams alerting, metrics, dashboard, caching |
| `AGM/modules/Unity.Flex/**` | [`flex/`](flex/README.md) | Dynamic forms/scoring engine: domain model, app services, web UI, DataGrid, styling & classification |
| `AGM/src/Unity.GrantManager.Domain.Shared/Flex/**`<br>`AGM/src/Unity.GrantManager.Domain/Intakes/CustomFieldsIntakeSubmissionMapper.cs`<br>`AGM/src/Unity.GrantManager.Application/Assessments/AssessmentScoresheetService.cs` | [`flex/flex-integration.md`](flex/flex-integration.md) | Host-side Flex consumption points and call sites |
| `AGM/modules/Unity.TenantManagement/**` | [`tenant-management/`](tenant-management/README.md) | Tenant creation/provisioning/config, Onboarding queue, web UI, permission gating |
| `AGM/src/Unity.GrantManager.Application/TenantManagement/**`<br>`AGM/src/Unity.GrantManager.Application/Identity/CssOnboardingUserLookup.cs`<br>`AGM/src/Unity.GrantManager.Domain/Applications/OnboardingApplicationManager.cs` | [`tenant-management/tenant-management-onboarding.md`](tenant-management/tenant-management-onboarding.md) | Host-side onboarding provider, core-field registry, IDIR user lookup, onboarding status workflow |
| `AGM/src/Unity.GrantManager.Application/Tenants/PostCreation/**`<br>`AGM/modules/Unity.SharedKernel/PostTenantCreation/**` | [`tenant-management/tenant-management-post-creation.md`](tenant-management/tenant-management-post-creation.md) | Deferred post-tenant-creation job sequence and status tracking |
| `AGM/modules/Unity.Reporting/**`<br>`AGM/src/Unity.GrantManager.Application/Reporting/**`<br>`AGM/modules/Unity.Flex/src/Unity.Flex.Application/Reporting/**` | [`reporting/`](reporting/reporting-architecture.md) | Reporting layers, view generation, configuration, and the `get_*_data` view specifications |
| `AGM/modules/Unity.AI/**` | [`ai/`](ai/README.md) | The six AI operations, the Azure OpenAI runtime, prompt families and versioning, feature/setting/permission gating, web UI |
| `AGM/src/Unity.GrantManager.Application/GrantApplications/Automation/**`<br>`AGM/src/Unity.GrantManager.Domain/GrantApplications/AIGenerationRequest.cs`<br>`AGM/src/Unity.GrantManager.Domain/Applications/ApplicationScoresheetAnswers.cs`<br>`AGM/src/Unity.GrantManager.Domain/ApplicationForms/GenerationReview.cs` | [`ai/ai-generation-pipeline.md`](ai/ai-generation-pipeline.md)<br>[`ai/ai-operations.md`](ai/ai-operations.md) | Host-side generation queue, background job, the six operation executors, prerequisite validation, and the AI request/review entities |
| `AGM/modules/Unity.Payments/**` | [`payments/`](payments/README.md) | Payment requests, the multi-level approval state machine, CAS invoice/reconciliation integration, suppliers and sites, FSB notifications, web UI |
| `AGM/src/Unity.GrantManager.Application/Payments/**`<br>`AGM/src/Unity.GrantManager.Application/Applicants/ApplicantSupplierAppService.cs`<br>`AGM/src/Unity.GrantManager.Application/ApplicantProfile/Payments/**`<br>`AGM/src/Unity.GrantManager.Domain.Shared/Payments/PaymentConsts.cs` | [`payments/README.md`](payments/README.md)<br>[`payments/payments-suppliers-and-sites.md`](payments/payments-suppliers-and-sites.md) | Host-side account coding, payment thresholds, payment settings, applicant↔supplier linking, and the applicant payment summary |
| `AGM/modules/Unity.Notifications/**` | [`notifications/`](notifications/README.md) | Email pipeline (event → log → queue → CHES), S3 attachments, templates, SignalR realtime messaging and notification logs, web UI |
| `AGM/src/Unity.GrantManager.Application/Notifications/**`<br>`AGM/src/Unity.GrantManager.Application/Events/ScheduledNotification*.cs`<br>`AGM/src/Unity.GrantManager.Application/Events/DateBasedScheduledNotificationJob.cs`<br>`AGM/src/Unity.GrantManager.Domain/Notifications/**` | [`notifications/notifications-scheduled-notifications.md`](notifications/notifications-scheduled-notifications.md)<br>[`notifications/notifications-email-pipeline.md`](notifications/notifications-email-pipeline.md) | Host-side send/list app services, the `ScheduledNotification` configuration entity, and its event- and date-driven triggers |
| `AGM/src/Unity.GrantManager.Application/ApplicantProfile/**`<br>`AGM/src/Unity.GrantManager.Application.Contracts/ApplicantProfile/**`<br>`AGM/src/Unity.GrantManager.Domain.Shared/ApplicantProfile/**` | [`applicant-portal/applicant-profile-data-providers.md`](applicant-portal/applicant-profile-data-providers.md) | The polymorphic profile endpoint and its `IApplicantProfileDataProvider` strategy set |
| `AGM/src/Unity.GrantManager.Application/Messaging/**`<br>`AGM/src/Unity.GrantManager.Domain/Messaging/**` | [`transactional-outbox-pattern.md`](transactional-outbox-pattern.md)<br>[`applicant-portal/grants-portal-rabbitmq-integration.md`](applicant-portal/grants-portal-rabbitmq-integration.md) | Inbox/outbox entities and workers; the RabbitMQ consumer pipeline and Quartz job coordination |
| `AGM/src/Unity.GrantManager.HttpApi/**` (applicant-portal-facing controllers only) | [`applicant-portal/applicant-portal-integration.md`](applicant-portal/applicant-portal-integration.md) | REST + messaging contract between Grant Manager and the Applicant Portal |
| `.github/workflows/sonarsource-scan.yml`<br>`AGM/sonar-project.properties` | [`SonarCloudAnalysis/`](SonarCloudAnalysis/SonarCloud_Setup_Guide.md)<br>[`unity-sonarcloud-readme.md`](unity-sonarcloud-readme.md) | SonarCloud setup, maintenance, and the transition from SonarQube |

## Cross-cutting docs

These are not tied to a single source path — check them when a change alters an external integration or dependency:

- [`External Dependency Summary.md`](External%20Dependency%20Summary.md) and [`External-Dependency-Chart.md`](External-Dependency-Chart.md) — the external systems Unity Portal talks to (CHEFS, CAS, Metabase, Keycloak/CSS, RabbitMQ). Update when an integration is added, removed, or repointed.
- [`flex/flex-roadmap.md`](flex/flex-roadmap.md), [`tenant-management/tenant-management-roadmap.md`](tenant-management/tenant-management-roadmap.md), [`notifications/notifications-roadmap.md`](notifications/notifications-roadmap.md), [`payments/payments-roadmap.md`](payments/payments-roadmap.md), [`ai/ai-roadmap.md`](ai/ai-roadmap.md) — known gaps and rough edges. If you *fix* something listed there, remove it from the roadmap.

## Undocumented areas

No documentation exists for these modules. That is a deliberate gap, not an oversight to fix in passing — do not auto-generate docs for them:

`Unity.Identity.Web` · `Unity.Theme.UX2` · most of `Unity.SharedKernel`

If you believe one of these genuinely needs documenting, raise it as its own ticket rather than bundling it into an unrelated change.

## Conventions for these docs

- Each feature folder has a `README.md` that gives a **reading order** and a **source-location map**. If you add a doc to a folder, add it to that folder's README too.
- Docs describe *how the system works and why*, with concrete file paths and class names — not changelogs. Don't add "as of <date>" or "recently changed" phrasing; it rots.
- When you cite a file path or class name, make sure it exists at that path. Stale paths are the most common way these docs go wrong.
