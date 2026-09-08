# Unity.Flex Module Documentation

Unity.Flex is Unity Portal's **dynamic/configurable-forms and scoring engine**. It lets program staff define custom intake fields ("worksheets") and assessor scoresheets ("scoresheets") through an admin UI — without code changes or deployments — and attach instances of them to grant applications (and, generically, any other entity) at points the core `Unity.GrantManager` module chooses: intake submission, the Project Info tab, the Assessment tab, the Funding Agreement tab, or a fully custom tab.

This folder documents how the module is built and how it is used. Read in this order:

1. **[flex-overview.md](flex-overview.md)** — what problem Flex solves, module layout, dependency direction, core concepts glossary.
2. **[flex-domain-model.md](flex-domain-model.md)** — entities, aggregate roots, relationships, field/question types, validation rules, database schema.
3. **[flex-application-services.md](flex-application-services.md)** — app service APIs, controllers, the local-event/handler pattern that decouples Flex from the host module.
4. **[flex-integration.md](flex-integration.md)** — how `Unity.GrantManager` (intake, applications, assessment) and `Unity.Reporting` actually consume Flex, with concrete call sites.
5. **[flex-web-ui.md](flex-web-ui.md)** — the admin builder screens, runtime fill-in widgets, and how permissions/features gate access.
6. **[flex-styling-and-classification.md](flex-styling-and-classification.md)** — per-field presentation (label position, inline style, CSS class), validation constraints (min/max/length) by type, and the `SecurityClassification` (Protected A/B/C) property — what's fully wired versus captured-but-unused.
7. **[flex-datagrid.md](flex-datagrid.md)** — the DataGrid field type in depth: explicit vs. dynamic (CHEFS-driven) vs. combined column population, editing, auto-sum, and its distinct reporting behavior.
8. **[flex-roadmap.md](flex-roadmap.md)** — aspirational: bringing permission-aware definitions to worksheets/scoresheets, and how the existing Zone system (and the already-present `SecurityClassification` field) is relevant prior art.

## Source location

```
applications/Unity.GrantManager/modules/Unity.Flex/
├── src/
│   ├── Unity.Flex.Domain.Shared/           (not present — shared enums live in Unity.Flex.Shared)
│   ├── Unity.Flex.Shared/                  field/question type enums, definitions, values, ChefsToUnityTypes
│   ├── Unity.Flex.Application.Contracts/   DTOs + app service interfaces
│   ├── Unity.Flex.Application/             entities, domain services, EF Core, app services, controllers, handlers, reporting generators
│   └── Unity.Flex.Web/                     Razor Pages (admin builder) + ViewComponents (runtime widgets)
└── test/
    ├── Unity.Flex.TestBase/
    ├── Unity.Flex.Application.Tests/
    └── Unity.Flex.Web.Tests/
```

Host-side integration points live in `applications/Unity.GrantManager/src/Unity.GrantManager.Domain.Shared/Flex/FlexConsts.cs`, `Unity.GrantManager.Domain/Intakes/CustomFieldsIntakeSubmissionMapper.cs`, `Unity.GrantManager.Application/GrantApplications/GrantApplicationAppService.cs`, and `Unity.GrantManager.Application/Assessments/AssessmentScoresheetService.cs`.

A related one-page visual summary is in `documentation/handover/flex-module-handover.html`.
