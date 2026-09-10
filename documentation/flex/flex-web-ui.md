# Flex Web UI

## Admin/config side — building worksheets and scoresheets

`Unity.Flex.Web/Pages/`:

- **`WorksheetConfiguration/Index.cshtml(.cs)`** + modals `UpsertWorksheetModal`, `UpsertSectionModal`, `UpsertCustomFieldModal`, `CloneWorksheetModal`, `PublishWorksheetModal` — the builder UI for defining worksheets → sections → custom fields, including drag/reorder (`ResequenceCustomFieldsAsync`, `ResequenceSectionsAsync`, `MoveToSectionAsync`) and JSON export/import and the publish/archive lifecycle.
- **`ScoresheetConfiguration/Index.cshtml(.cs)`** + modals `ScoresheetModal`, `SectionModal`, `QuestionModal`, `CloneScoresheetModal`, `PublishScoresheetModal` — the equivalent builder for scoresheets → sections → questions.
- **`Components/DataGrid/`** (`DataGridReadService`, `DataGridWriteService`, `EditDataRowModal`) — supports the `DataGrid` custom-field type (a repeating-row grid field).

These Razor Pages are hosted **inside** the main app's Configuration Management screen, not as a standalone area: `Unity.GrantManager.Web/Pages/ConfigurationManagement/Index.cshtml` + `ScoresheetConfiguration.js` embed/link to them. `ConfigurationManagement/Index.cshtml.cs` requires `UnitySettingManagementPermissions.UserInterface` and computes per-section visibility flags — `ShowCustomFields` and `ShowScoresheets` both resolve to `await featureChecker.IsEnabledAsync("Unity.Flex")`.

## Runtime/fill-in side — rendering and capturing instance data

A large library of paired Razor **ViewComponents** under `Views/Shared/Components/`, one pair per field/question type:

- **Definition widgets** — used in the builder to configure a field's constraints.
- **Value widgets** — used to render/capture an instance's value.

Per type: Text, TextArea, Numeric, Currency, Date, DateTime (via a generic `DefaultFieldWidget`), Checkbox, CheckboxGroup, Radio, SelectList, YesNo, DataGrid, BCAddress (BC Address lookup widget) — plus `CustomFieldDefinitionWidget` / `QuestionDefinitionWidget` as type-dispatching wrappers.

**Composite components:**

- `WorksheetWidget` / `WorksheetInstanceWidget` — renders a full worksheet's sections/fields bound to a `WorksheetInstance`, backed by `WorksheetSectionRenderModel` / `WorksheetViewModel` view models.
- `WorksheetListWidget` — lists the worksheets mounted at a given UI anchor.
- `WorksheetConfiguration` / `ScoresheetConfiguration` ViewComponents — embed the builder into a host page.
- `Scoresheet` component (`Scoresheet.js` / `.css`) — the assessor-facing scoring UI.
- `CustomTabWidget` — renders a Flex worksheet as a fully custom application tab (ties back to `FlexConsts.CustomTab`).

## Permissions and feature gating

Flex has **no permission definitions of its own** — `FlexMenus.cs` is an empty stub with no menu items registered. Access control is entirely delegated to the host application:

| Layer | Gate |
|---|---|
| Configuration Management page (builders) | Host permission `UnitySettingManagementPermissions.UserInterface` |
| Custom Fields / Scoresheets sections within it | ABP tenant feature `"Unity.Flex"` |
| Reporting sync app services (IT tooling) | `[Authorize(IdentityConsts.ITAdminPolicyName)]` |
| Everything else in the Flex module | No explicit `[Authorize]` — relies on the caller (host app service or Razor Page) already having checked the relevant host permission |

A tenant can therefore turn the entire Flex-driven UI off by disabling the `"Unity.Flex"` feature; see [flex-integration.md](flex-integration.md#assessment--scoresheet-scoring) for how the assessment flow falls back to a legacy mechanism in that case.
