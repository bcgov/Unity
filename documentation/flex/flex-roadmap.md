# Flex Roadmap: Permission-Aware Definitions

> **Status: aspirational / not started.** This document captures a stated future direction, not a committed design. It exists to give the next person picking this up a starting point and the relevant prior art already in the codebase — not a spec to implement as-is.

## The vision

Today, Flex has **no permission model of its own** (see [flex-web-ui.md](flex-web-ui.md#permissions-and-feature-gating)). A worksheet or scoresheet, once mounted to a UI anchor, is visible to anyone who can see that anchor's tab — gated only by two coarse, all-or-nothing switches: the host page's `UnitySettingManagementPermissions.UserInterface` permission (for the builder) and the `Unity.Flex` tenant feature flag (for everything else). There is no way to say "this section is only visible to assessors" or "this field is read-only for external reviewers" without writing host-side code.

The grand vision is to make **permission-awareness a first-class part of the Worksheet/Scoresheet definition itself** — so a program admin building a form in the Configuration UI could attach a permission requirement directly to a worksheet, a section, or an individual field (and the scoresheet equivalents), the same way they already set a field's type, label, or required-ness. The rest of the system — rendering widgets, app services — would then respect that requirement automatically, without any host-side code change per program.

## Why this isn't a green-field problem

Two pieces of prior art already in this codebase solve adjacent problems and are the natural starting point rather than inventing a parallel mechanism.

### 1. The ABP permission system (already used everywhere else)

Every other module in this codebase gates access through `[Authorize(PermissionName)]` on app services and `IPermissionChecker.IsGrantedAsync(permissionName)` elsewhere, with permissions declared in a `*PermissionDefinitionProvider` (see `.claude/rules/csharp.md` and `.claude/rules/security.md`: *"Apply `[Authorize(PermissionName)]` attributes on all application service methods... Never rely solely on UI-level permission hiding — always enforce server-side."*). Flex is the odd one out: `FlexMenus.cs` is an empty stub and no `FlexPermissionDefinitionProvider` exists anywhere in the module.

### 2. The Zone system (`Unity.GrantManager.Domain/Zones`, `Unity.GrantManager.Web/TagHelpers/Zone`) — a proven, adjacent pattern

The host application already solves a close cousin of this problem for its own hardcoded UI: the Application Details page is built from named **tabs** and **zones** (`ZoneTabDefinition`, `ZoneDefinition`), each of which can be toggled on/off **per form** by an admin, stored as a JSON `ZoneGroupDefinition` setting via `ZoneManager`/`ZoneManagementAppService` (`SettingsConstants.UI.Zones`, keyed by provider `"F"` + form id, falling back to `DefaultZoneDefinition.Template`). Rendering is gated by a `<zone>` / `<zone-fieldset>` Razor tag helper (`UnityZoneTagHelper` / `UnityZoneTagHelperService`) that layers **three independent checks**:

| Check | Source | What it answers |
|---|---|---|
| Feature | `IFeatureChecker.IsEnabledAsync(FeatureRequirement)` | Is this capability turned on for the tenant at all? |
| Zone toggle | `IZoneChecker.IsEnabledAsync(ZoneRequirement, formId)` | Has an admin enabled this specific tab/zone for this form? |
| Permission | `IPermissionChecker.IsGrantedAsync(PermissionRequirement)` | Does *this user* have the ABP permission to see it? |

`ZoneRequirementType` (`Full`, `ToggleOnly`, `PermissionOnly`) lets a given zone opt out of the toggle or the permission check independently. The tag helper also separates **read** from **update**: `PermissionRequirement`/`ReadCondition` gate visibility, `UpdatePermissionRequirement`/`UpdateCondition` additionally gate whether a `<zone-fieldset>` renders `disabled`. It even ships a debug overlay (`AppendDebugHeader`) that shows pass/fail per requirement per zone — useful for support/troubleshooting, and a UX pattern worth carrying forward.

This is exactly the shape of problem Flex will eventually have — "is this UI region visible, and can this user edit it, on a per-form/per-instance basis" — solved once already, in the same codebase, with real production usage. The Flex roadmap should build on this pattern rather than reinvent it.

## What "leveraging the zone system" could concretely mean

Roughly in order of how much new machinery each option requires:

1. **Copy the pattern, not the code.** Give `WorksheetSection`/`CustomField` (and `ScoresheetSection`/`Question`) their own optional `ReadPermission` / `UpdatePermission` properties, and have the runtime widgets (`WorksheetWidget`, `WorksheetInstanceWidget`, `CustomTabWidget`, the `Scoresheet` component) run the same three-layer check inline, using `IPermissionChecker` directly. Lowest coupling to the existing Zone code, but duplicates its logic.
2. **Register Flex sections as real zones.** Have each published `WorksheetSection` (or `Worksheet` itself) register an entry in the `ZoneGroupDefinition` for its host form, so the *existing* `ZoneManagementAppService` admin screen and `<zone>` tag helper drive Flex section visibility too — one governance surface for both hardcoded and dynamic UI, instead of two. Requires bridging Flex's per-worksheet-instance correlation model with the Zone system's per-form model (Zones are keyed by form id today, not by worksheet or worksheet-instance id).
3. **Extend `IZoneChecker`/`ZoneManager` to be correlation-aware**, so a zone-like check can be scoped to `(correlationId, correlationProvider)` the way `WorksheetInstance` already is, rather than only to a form id. This would let the same mechanism cover both the host's hardcoded tabs and Flex's dynamic content uniformly.

None of these has been scoped in detail — they're listed to show the range from "small, local change" to "unify the two systems," not to pick a winner.

## A head start that already exists: `SecurityClassification`

This is worth calling out before the open questions below, because it changes the shape of the problem: every `CustomFieldDefinition` already carries a `SecurityClassification` property (`ProtectedA`/`ProtectedB`/`ProtectedC`, mirroring the BC Government's real information classification standard), and the Worksheet Configuration builder already has a UI for setting it, complete with explanatory tooltips per level. See [flex-styling-and-classification.md](flex-styling-and-classification.md#security-classification--protected-abc) for the full detail.

**It is currently presentation metadata only — captured and stored, but never read by anything.** Not checked by any app service or permission, not shown to end users at runtime, not factored into reporting. This is precisely the "sensitivity tier" concept the open question below reasons toward from scratch — except it doesn't need to be designed, it needs to be *wired up*: a real enforcement layer (server-side checks keyed off this field, and some runtime UI treatment) is the missing piece, not the tiering concept itself.

## Open design questions to resolve before building this

- **Static permissions vs. runtime-created definitions.** ABP permissions are declared at compile time via `*PermissionDefinitionProvider`. Worksheets, sections, and fields are created at runtime by a program admin. A literal "one ABP permission per field" model doesn't fit ABP's normal registration flow. The existing `SecurityClassification` tiers are the natural fit here: map each of `ProtectedA`/`ProtectedB`/`ProtectedC` (plus "none") to a real, statically-declared ABP permission, and enforcement becomes "does this user hold the permission for this field's tier" rather than "mint a permission per field."
- **Server-side enforcement, not just UI hiding.** `.claude/rules/security.md` is explicit: *"Never rely solely on UI-level permission hiding — always enforce server-side."* Whatever gets attached to a definition must also be checked in the app services that read/write instance data (`ICustomFieldValueAppService`, `IWorksheetAppService`, `IScoresheetInstanceAppService`, etc.), not only in the rendering widgets — otherwise the API remains an unguarded backdoor around a UI-only restriction.
- **Read/write split.** Mirror the Zone tag helper's `PermissionRequirement` vs. `UpdatePermissionRequirement` — a field being visible to a role doesn't mean that role can edit it (e.g. an assessor's own scoring notes visible-but-locked to an external reviewer).
- **Backward compatibility.** Every worksheet/scoresheet published today has no permission attached to any section or field. The default (absent) state must continue to mean "visible/editable to anyone who already has host-level access to the anchor" — this has to be additive, not a breaking migration.
- **Admin UX.** The Worksheet/Scoresheet Configuration builder screens would need a permission (or sensitivity-tier) picker per section/field. Exposing the full ABP permission tree would be overwhelming and error-prone; a curated, Flex-scoped list is likely necessary.
- **Reporting implications.** The reporting pipeline (`ReportingDataGeneratorService`, dynamic SQL views — see [flex-application-services.md](flex-application-services.md#reporting-integration)) currently flattens *all* answers/values into report data uniformly. If a field becomes permission-gated, the reporting layer needs its own story for whether restricted data flows into shared reports at all, and if so, whether the view itself needs row/column-level security — this has downstream implications for `Unity.Reporting` and Metabase consumers, not just the Flex UI.

## Suggested next step

Before writing code: a short spike comparing option 1 (copy the pattern) against option 2 (register Flex sections as real zones) against a small number of real worksheets/scoresheets currently in use, to see whether form-level granularity (what Zones give today) is actually sufficient, or whether section/field-level granularity is a hard requirement — that answer changes which of the three options above is worth pursuing.
