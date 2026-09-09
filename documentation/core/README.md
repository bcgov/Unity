# Unity Portal Core Documentation

This folder documents **the core Grant Manager application** — everything in `applications/Unity.GrantManager/src/` that is not one of the feature modules under `modules/`.

The core is where a grant application is received, moved through review and assessment, and decided. Every module hangs off it: Flex adds custom fields to its forms, Payments pays out against its approved amounts, AI drafts analysis and scoring for it, Notifications emails about it, Reporting reads it.

It is also, by a wide margin, the largest thing in the repository — roughly **62,000 lines of C# across nine projects**, five to six times the size of the largest module. It is documented as a set of small, focused documents grouped into tracks rather than as one folder-sized narrative.

## Track A · Intake and assessment

The business spine: how an application gets in, how it moves, and how it gets decided.

1. **[core-overview.md](core-overview.md)** — project layering, the two databases, multi-tenancy, how modules compose into the host, the request path, core concepts glossary.
2. **[core-intake.md](core-intake.md)** — the CHEFS webhook, submission validation, field mapping, application creation, Unity application IDs, the events intake publishes, and the nightly resync worker.
3. **[core-application-lifecycle.md](core-application-lifecycle.md)** — the `Application` aggregate and its **hierarchical 15-state** workflow, permitted actions, direct approval, assignments, owners, links, and the field-update permission tiers.
4. **[core-assessment.md](core-assessment.md)** — the `Assessment` aggregate and its 3-state workflow, creation rules, scoresheet instances, AI assessments, and assessment results.
5. **[core-applicants.md](core-applicants.md)** — the `Applicant` aggregate, applicant agents, addresses, contacts, lookup, and the merge/unmerge operation.
6. **[core-forms-and-versions.md](core-forms-and-versions.md)** — `ApplicationForm` and `ApplicationFormVersion`, the submission-header mapping, CHEFS synchronisation, and the per-form flags that change behaviour across the whole product.

## Track B · Collaboration

What people attach to an application and record around it.

7. **[core-attachments.md](core-attachments.md)** — the two storage paths (CHEFS-held and S3), the entity hierarchy, preview and PDF conversion, and attachment resync.
8. **[core-comments-and-tags.md](core-comments-and-tags.md)** — comment threads on three subjects, @-mention email, the shared tag table, and the two-event tag-deletion protocol.
9. **[core-history-and-audit.md](core-history-and-audit.md)** — the three things called "history", how change history is read from ABP's audit log, and the hard-coded type names behind payment history.

## Track C · Infrastructure

The platform the spine runs on.

10. **[core-persistence.md](core-persistence.md)** — the two DbContexts, the migration split, per-tenant database provisioning, multi-tenancy, repositories, and the startup warm-up.
11. **[core-permissions-and-identity.md](core-permissions-and-identity.md)** — the `UnitySelector` tree, override permissions, roles and seeding, Keycloak/CSS, and the five places authorization is applied.
12. **[core-zones-and-ui-config.md](core-zones-and-ui-config.md)** — zones as configuration *and* permission, the default template, the `<zone>` tag helper's four gates, and the zone debugger.
13. **[core-integrations.md](core-integrations.md)** — dynamic URLs, resilient HTTP, and the CHEFS, CSS, OrgBook, Geocoder, Metabase, Matomo and GitHub clients.
14. **[core-web-shell.md](core-web-shell.md)** — `GrantManagerWebModule`, the request pipeline, custom middleware, menus, pages, the 36 shared view components, theming.
15. **[core-background-and-ops.md](core-background-and-ops.md)** — Quartz workers and the per-tenant sweep, distributed locking, health checks, exception logging and alerting, metrics, dashboard, auditing, caching.

## Source location

```
applications/Unity.GrantManager/src/
├── Unity.GrantManager.Domain.Shared/        enums, state and action types, error codes,
│                                              UnityWorkflow, localization, feature/setting keys
├── Unity.GrantManager.Domain/               aggregates, domain managers, repository interfaces,
│                                              permission seeding, zones
├── Unity.GrantManager.Application.Contracts/ DTOs, app service interfaces, permission definitions
├── Unity.GrantManager.Application/          app services, intake, integrations, events,
│                                              background workers, attachments, dashboard
├── Unity.GrantManager.EntityFrameworkCore/  both DbContexts, repositories, host + tenant migrations
├── Unity.GrantManager.HttpApi/              external-facing controllers (CHEFS, portal, attachments)
├── Unity.GrantManager.Web/                  Razor Pages, view components, menus, middleware, theme
├── Unity.GrantManager.HttpApi.Client/       generated client proxies
└── Unity.GrantManager.DbMigrator/           migration + seeding host
```

| Project | Lines | Files |
|---|---:|---:|
| `Application` | 26,232 | 221 |
| `Web` | 15,008 | 226 |
| `Domain` | 8,000 | 168 |
| `Application.Contracts` | 5,671 | 292 |
| `EntityFrameworkCore` | 4,421 | 59 |
| `HttpApi` · `Domain.Shared` · `DbMigrator` · `HttpApi.Client` | ~3,100 | 82 |

## Core code documented elsewhere

Several parts of the core belong to a module's story and are documented with that module. These docs cross-link rather than repeat:

| Core path | Documented in |
|---|---|
| `Application/Reporting/**` | [`reporting/`](../reporting/README.md) |
| `Application/Messaging/**`, `Application/GrantsPortal/**`, `Domain/Messaging/**` | [`transactional-outbox-pattern.md`](../transactional-outbox-pattern.md), [`applicant-portal/grants-portal-rabbitmq-integration.md`](../applicant-portal/grants-portal-rabbitmq-integration.md) |
| `Application/ApplicantProfile/**` | [`applicant-portal/applicant-profile-data-providers.md`](../applicant-portal/applicant-profile-data-providers.md) |
| `HttpApi/**` (portal-facing controllers) | [`applicant-portal/applicant-portal-integration.md`](../applicant-portal/applicant-portal-integration.md) |
| `Application/TenantManagement/**`, `Application/Tenants/PostCreation/**` | [`tenant-management/`](../tenant-management/README.md) |
| `Application/Notifications/**`, `Application/Events/ScheduledNotification*` | [`notifications/`](../notifications/README.md) |
| `Application/Payments/**`, `Applicants/ApplicantSupplierAppService.cs` | [`payments/`](../payments/README.md) |
| `Application/GrantApplications/Automation/**` | [`ai/`](../ai/README.md) |
| `Domain.Shared/Flex/**`, `Intakes/CustomFieldsIntakeSubmissionMapper.cs` | [`flex/flex-integration.md`](../flex/flex-integration.md) |
