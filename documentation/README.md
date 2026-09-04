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
| `AGM/modules/Unity.Flex/**` | [`flex/`](flex/README.md) | Dynamic forms/scoring engine: domain model, app services, web UI, DataGrid, styling & classification |
| `AGM/src/Unity.GrantManager.Domain.Shared/Flex/**`<br>`AGM/src/Unity.GrantManager.Domain/Intakes/CustomFieldsIntakeSubmissionMapper.cs`<br>`AGM/src/Unity.GrantManager.Application/Assessments/AssessmentScoresheetService.cs` | [`flex/flex-integration.md`](flex/flex-integration.md) | Host-side Flex consumption points and call sites |
| `AGM/modules/Unity.TenantManagement/**` | [`tenant-management/`](tenant-management/README.md) | Tenant creation/provisioning/config, Onboarding queue, web UI, permission gating |
| `AGM/src/Unity.GrantManager.Application/TenantManagement/**`<br>`AGM/src/Unity.GrantManager.Application/Identity/CssOnboardingUserLookup.cs`<br>`AGM/src/Unity.GrantManager.Domain/Applications/OnboardingApplicationManager.cs` | [`tenant-management/tenant-management-onboarding.md`](tenant-management/tenant-management-onboarding.md) | Host-side onboarding provider, core-field registry, IDIR user lookup, onboarding status workflow |
| `AGM/src/Unity.GrantManager.Application/Tenants/PostCreation/**`<br>`AGM/modules/Unity.SharedKernel/PostTenantCreation/**` | [`tenant-management/tenant-management-post-creation.md`](tenant-management/tenant-management-post-creation.md) | Deferred post-tenant-creation job sequence and status tracking |
| `AGM/modules/Unity.Reporting/**`<br>`AGM/src/Unity.GrantManager.Application/Reporting/**`<br>`AGM/modules/Unity.Flex/src/Unity.Flex.Application/Reporting/**` | [`reporting/`](reporting/reporting-architecture.md) | Reporting layers, view generation, configuration, and the `get_*_data` view specifications |
| `AGM/src/Unity.GrantManager.Application/ApplicantProfile/**`<br>`AGM/src/Unity.GrantManager.Application.Contracts/ApplicantProfile/**`<br>`AGM/src/Unity.GrantManager.Domain.Shared/ApplicantProfile/**` | [`applicant-portal/applicant-profile-data-providers.md`](applicant-portal/applicant-profile-data-providers.md) | The polymorphic profile endpoint and its `IApplicantProfileDataProvider` strategy set |
| `AGM/src/Unity.GrantManager.Application/Messaging/**`<br>`AGM/src/Unity.GrantManager.Domain/Messaging/**` | [`transactional-outbox-pattern.md`](transactional-outbox-pattern.md)<br>[`applicant-portal/grants-portal-rabbitmq-integration.md`](applicant-portal/grants-portal-rabbitmq-integration.md) | Inbox/outbox entities and workers; the RabbitMQ consumer pipeline and Quartz job coordination |
| `AGM/src/Unity.GrantManager.HttpApi/**` (applicant-portal-facing controllers only) | [`applicant-portal/applicant-portal-integration.md`](applicant-portal/applicant-portal-integration.md) | REST + messaging contract between Grant Manager and the Applicant Portal |
| `.github/workflows/sonarsource-scan.yml`<br>`AGM/sonar-project.properties` | [`SonarCloudAnalysis/`](SonarCloudAnalysis/SonarCloud_Setup_Guide.md)<br>[`unity-sonarcloud-readme.md`](unity-sonarcloud-readme.md) | SonarCloud setup, maintenance, and the transition from SonarQube |

## Cross-cutting docs

These are not tied to a single source path — check them when a change alters an external integration or dependency:

- [`External Dependency Summary.md`](External%20Dependency%20Summary.md) and [`External-Dependency-Chart.md`](External-Dependency-Chart.md) — the external systems Unity Portal talks to (CHEFS, CAS, Metabase, Keycloak/CSS, RabbitMQ). Update when an integration is added, removed, or repointed.
- [`flex/flex-roadmap.md`](flex/flex-roadmap.md), [`tenant-management/tenant-management-roadmap.md`](tenant-management/tenant-management-roadmap.md) — known gaps and rough edges. If you *fix* something listed there, remove it from the roadmap.

## Undocumented areas

No documentation exists for these modules. That is a deliberate gap, not an oversight to fix in passing — do not auto-generate docs for them:

`Unity.AI` · `Unity.Notifications` · `Unity.Payments` · `Unity.Identity.Web` · `Unity.Theme.UX2` · most of `Unity.SharedKernel` · the core `Unity.GrantManager` application/assessment/intake workflow

If you believe one of these genuinely needs documenting, raise it as its own ticket rather than bundling it into an unrelated change.

## Conventions for these docs

- Each feature folder has a `README.md` that gives a **reading order** and a **source-location map**. If you add a doc to a folder, add it to that folder's README too.
- Docs describe *how the system works and why*, with concrete file paths and class names — not changelogs. Don't add "as of <date>" or "recently changed" phrasing; it rots.
- When you cite a file path or class name, make sure it exists at that path. Stale paths are the most common way these docs go wrong.
