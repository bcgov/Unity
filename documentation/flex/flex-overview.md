# Flex Overview

## What problem it solves

Grant programs need custom intake questions and custom assessor scoring criteria, and those requirements change per-program and over time. Without Flex, every new question or scoring criterion would mean a code change, a migration, and a deployment. Unity.Flex turns "define a form field" and "define a scoring question" into an **admin-configurable, runtime action**: a program admin builds a **Worksheet** (custom data-collection form) or a **Scoresheet** (assessment/scoring form) through a UI, publishes it, and the rest of the system renders and stores data against it — no code change required.

It does this with a deliberately generic, reusable pattern that shows up twice in the module:

```text
Definition (template)  →  Instance (one filled-in occurrence)  →  Value (one field's data)
   Worksheet                  WorksheetInstance                      CustomFieldValue
   Scoresheet                 ScoresheetInstance                     Answer
```

Definitions and values are stored as JSON, not as fixed relational columns — that's what makes new fields/questions possible without a schema migration. See [flex-domain-model.md](flex-domain-model.md) for the details.

## Core concepts (glossary)

|Term|Meaning|
|---|---|
|**Worksheet**|An admin-defined custom data-collection form template (e.g. "Project Details"). Made of sections and fields.|
|**WorksheetSection**|A named, ordered group of fields within a Worksheet.|
|**CustomField**|One field definition (text, number, dropdown, etc.) within a section.|
|**WorksheetLink**|Attaches a Worksheet template to a specific UI location on an external entity (e.g. the Project Info tab of an Application).|
|**WorksheetInstance**|One filled-in occurrence of a Worksheet, tied to a specific target record (e.g. one Application).|
|**CustomFieldValue**|One field's value within a WorksheetInstance.|
|**Scoresheet**|An admin-defined assessment/scoring form template (e.g. "Standard Assessment Scoresheet"). Made of sections and questions.|
|**ScoresheetSection**|A named, ordered group of questions within a Scoresheet.|
|**Question**|One scoreable question within a section.|
|**ScoresheetInstance**|One assessor's completed response to a Scoresheet, tied to a specific assessment.|
|**Answer**|One question's answer within a ScoresheetInstance.|
|**UI Anchor**|A named slot on the host application's UI (Project Info, Applicant Info, Assessment Info, Funding Agreement Info, Payment Info, Custom Tab, Preview) that a worksheet can be mounted to.|
|**Correlation**|A generic `(CorrelationId, CorrelationProvider)` pair used instead of a hard foreign key, so Flex entities can attach to any external entity type (an Application today, potentially something else tomorrow) without Flex depending on that module.|

## Module layout and dependency direction

Unity.Flex is a self-contained ABP module (its own solution file, `Unity.Flex.abpsln`) referenced by the host `Unity.GrantManager` solution — not the other way around. It follows the standard ABP layering used across this codebase (see the root `unity-module-structure` skill):

```text
Unity.Flex.Shared                    (field/question enums, Definitions, Values, ChefsToUnityTypes — no ABP dependency)
        ↑
Unity.Flex.Application.Contracts     (DTOs, app service interfaces)
        ↑
Unity.Flex.Application               (entities, domain services, EF Core, app services, controllers, event handlers, reporting generators)
        ↑
Unity.Flex.Web                       (Razor Pages admin builder + ViewComponents runtime widgets)
```

`Unity.GrantManager.Application`/`.Domain` depend on `Unity.Flex.Application.Contracts` (and, for the ETO-publishing side, `Unity.Flex.Shared`) to talk to Flex — never the reverse. The two modules are decoupled at runtime through **ABP local events** rather than direct app-service calls; see [flex-application-services.md](flex-application-services.md#commandhandler-pattern).

Other modules that reference Flex types (seen in test-bin output): `Unity.Notifications`, `Unity.Payments`, `Unity.Reporting`.

## Feature flag, not permission

Unity.Flex has **no dedicated ABP permission definitions of its own** (`FlexMenus.cs` is an empty stub). Access is entirely delegated to the host application:

- The whole Configuration Management screen (where the worksheet/scoresheet builders live) requires the host's `UnitySettingManagementPermissions.UserInterface` permission.
- Each builder section's visibility is gated purely by the ABP **tenant feature** `"Unity.Flex"` being enabled (`IFeatureChecker.IsEnabledAsync("Unity.Flex")`) — checked throughout the host module before publishing any Flex-related event, and in the host's `ConfigurationManagement/Index.cshtml.cs`.
- The only explicit `[Authorize]` inside the Flex module itself guards the two Reporting sync app services (`IdentityConsts.ITAdminPolicyName`) — IT-admin-only maintenance/backfill tooling, not part of normal end-user flow.

This means a tenant can be switched off Flex entirely via the feature flag; the host module falls back to a legacy, hardcoded scoresheet mechanism when it is disabled (see [flex-integration.md](flex-integration.md#assessment--scoresheet-scoring)).
