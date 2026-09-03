# Flex Field Styling & Classification

Every `CustomFieldDefinition` (the base class every field-type definition inherits — see [flex-domain-model.md](flex-domain-model.md#field-types-and-question-types)) carries a set of presentation and governance properties that are independent of the field's data type. This document covers those properties specifically: where they're set, where they're actually applied, and — importantly — which of them are fully wired end-to-end versus captured-but-unused today.

## The base properties

`CustomFieldDefinition` (`Unity.Flex.Shared/Worksheets/Definitions/CustomFieldDefinition.cs`):

|Property|Type|Default|Purpose|
|---|---|---|---|
|`Required`|bool|`false`|Feeds required-field validation (worksheets) / required-answer validation (scoresheets — see [flex-domain-model.md](flex-domain-model.md#required-answer-validation)).|
|`IsHidden`|bool|`false`|Renders the field's wrapper `hidden`.|
|`HideLabel`|bool|`false`|Suppresses the `<label>` element entirely.|
|`IsDisabled`|bool|`false`|Renders the field's `<fieldset>` `disabled`.|
|`LabelPosition`|string|`"Top"`|Only `"Top"` and `"Left"` are wired in the builder UI, though the property itself is a free-form string.|
|`Style`|string?|`null`|Inline CSS applied to the field's **wrapper `<div>`** (not the control itself).|
|`CssClass`|string?|`null`|Extra CSS class(es) appended to the field's wrapper `<div>`.|
|`LabelStyle`|string?|`null`|Inline CSS applied to the `<label>`.|
|`LabelCssClass`|string?|`null`|Extra CSS class(es) appended to the `<label>`.|
|`SecurityClassification`|string?|`null`|One of `ProtectedA` / `ProtectedB` / `ProtectedC`, or unset. See [Security classification](#security-classification--protected-abc) below.|
|`Placeholder`|string?|`null`|Placeholder text — only rendered by widgets for text-like types (see the builder's `placeholderSupportedTypes` set: Text, TextArea, Numeric, Currency, Email, Phone).|

## Where it's set — the builder UI

`UpsertCustomFieldModal.cshtml` / `.cshtml.cs` (`Unity.Flex.Web/Pages/WorksheetConfiguration/`) is the admin UI for all of this, split across two tabs:

- **Display tab** — Key, Label, a **Label Position** toggle (Top/Left button group), the field Type dropdown, a Placeholder input (shown only for text-like types via `UpsertCustomFieldModal.js`'s `placeholderSupportedTypes`), and the type-specific definition editor (min/max/options/etc., via `CustomFieldDefinitionWidget`).
- **Attributes tab**, three sections:
  - **Security** — a *Classification Level* dropdown (`— None —` / Protected A / Protected B / Protected C) with a live tooltip hint (see below).
  - **Visibility** — `IsHidden`, `HideLabel`, `IsDisabled` checkboxes.
  - **Label** — *Inline Style* and *CSS Class* text inputs.

**Gap worth knowing:** the Attributes tab's Label section only exposes `LabelStyle`/`LabelCssClass`. There is **no builder UI for the base `Style`/`CssClass` properties** (the ones that style the field's wrapper, not just its label) — the rendering pipeline fully supports them (see below), but today the only way to set them is to hand-edit a worksheet's exported/imported JSON. If "add CSS styling to the control itself" is on the table, wiring these two into the Attributes tab is the smallest, most direct next step — the plumbing already exists end to end except for the input fields themselves.

## Where it's applied — rendering

All of the above is applied in exactly **one place**, uniformly across every field type, before control is handed off to the type-specific widget: `_WorksheetSections.cshtml` (`Unity.Flex.Web/Views/Shared/Components/WorksheetInstanceWidget/`):

```text
labelPositionClass = fieldDef.LabelPosition == "Left" ? "label-left" : "label-top"
fieldExtraCssClass = fieldDef.CssClass (appended to the wrapper's class list)

<div class="worksheet_field ... {labelPositionClass}{fieldExtraCssClass}"
     hidden="{fieldDef.IsHidden}"
     style="{fieldDef.Style}">

  <label class="field-label {fieldDef.LabelCssClass}" style="{fieldDef.LabelStyle}">{field.Label}</label>  <!-- unless HideLabel -->

  <fieldset disabled="{fieldDef.IsDisabled}">
    <!-- type-specific widget renders the actual control here -->
  </fieldset>
</div>
```

This is a deliberate separation, not an oversight: styling lives on the shared wrapper, and the thirteen-odd per-type widgets (`DefaultFieldWidget`, `CurrencyWidget`, `DateWidget`, `TextAreaWidget`, etc.) stay focused on rendering the bare control — none of them re-read `Style`/`CssClass`/`LabelPosition` themselves. One consequence: `Style`/`CssClass` land on the *wrapper*, not the `<input>`/`<select>`/`<textarea>` itself — targeting the bare control from custom CSS means reaching through the wrapper (e.g. `.worksheet_field.my-class input`), not styling it directly.

## Security classification — Protected A/B/C

The three options map directly onto the BC Government's information classification standard, with the exact tooltip copy baked into `UpsertCustomFieldModal.js`:

|Level|Shown as|Tooltip copy|
|---|---|---|
|`ProtectedA`|Protected A — Low Sensitivity|"If compromised, could cause limited or moderate injury to an individual or organisation — e.g. an exact salary figure or home address."|
|`ProtectedB`|Protected B — Medium Sensitivity|"Could cause serious injury if disclosed — e.g. Social Insurance Numbers, employment equity data, or personal health records."|
|`ProtectedC`|Protected C — High Sensitivity|"The most sensitive level — disclosure could cause extremely grave injury."|

**This is presentation metadata only — it is not enforced anywhere.** Confirmed by searching the whole solution: `SecurityClassification` appears in exactly three places — the `CustomFieldDefinition` property itself, the builder modal (`UpsertCustomFieldModal.cshtml`/`.cshtml.cs`), and the modal's client-side tooltip script. It is:

- **Not checked** by any app service, authorization attribute, or permission check.
- **Not displayed** anywhere at runtime — an assessor or applicant filling in a "Protected C" field sees no indication it's classified.
- **Not factored** into the reporting pipeline or export data (see [flex-application-services.md](flex-application-services.md#reporting-integration)) — a restricted field flows into report data the same as any other.

This is directly relevant to [flex-roadmap.md](flex-roadmap.md): the "sensitivity tier" idea proposed there as a way to make ABP's compile-time permission model fit runtime-created fields **already exists in the schema and the builder UI**, fully captured and stored — only the enforcement layer (server-side checks in the app services, and some runtime UI treatment) is missing. This changes the roadmap from "design a tiering concept" to "wire up an existing one."

## Validation constraints per field type

Beyond the base properties, several field types carry their own min/max/length constraints. Coverage is uneven — some are enforced as native HTML attributes, one is defined but never rendered anywhere:

|Type|Constraint properties|Enforced how|
|---|---|---|
|`Text`|`MinLength` (uint, default 0), `MaxLength` (uint, default `uint.MaxValue`)|Both rendered as native `minlength`/`maxlength` attributes by `DefaultFieldWidget`.|
|`TextArea`|`MinLength`, `MaxLength`, `Rows` (uint, default 0)|`MaxLength` → native `maxlength`; `Rows` → native `rows`. **`MinLength` is captured but never rendered** — `TextAreaWidget` doesn't emit a `minlength` attribute.|
|`Numeric`|`Min`/`Max` (long, default `long.MinValue`/`MaxValue`)|Rendered as native `min`/`max` attributes by `DefaultFieldWidget` (via `DefinitionResolver.ResolveMin`/`ResolveMax`).|
|`Currency`|`Min`/`Max` (decimal), `Format` (string)|Since the underlying `<input>` is `type="text"` (not `number`), `Min`/`Max` render only as `data-min`/`data-max` custom attributes on `CurrencyWidget` — enforcement, if any, is client-side JS, not native browser validation. `Format` is defined but not observed applied in the widget.|
|`Date` / `DateTime`|`Min`/`Max` (DateTime, default `DateTime.MinValue`/`MaxValue`), `Format` (string)|**Defined on `DateDefinition` but not enforced anywhere** — `DefinitionResolver.ResolveMin`/`ResolveMax` have no case for `DateDefinition`, and `DateWidget` doesn't read `Min`/`Max`/`Format` at all. Dead properties today.|
|`Radio`|`Options` (`List<RadioOption>` — `Value`, `Label`), `GroupLabel`|Constrains choices to the option list via the rendered control.|
|`CheckboxGroup`|`Options` (`List<CheckboxGroupDefinitionOption>` — `Key`, `Value` (bool, default-checked state), `Label`)|Same.|
|`SelectList`|`Options` (`List<SelectListOption>` — `Key`, `Value`)|Same.|
|`QuestionSelectList` *(scoresheet-only)*|`Options` (`List<QuestionSelectListOption>` — `Key`, `Value`, **`NumericValue`**)|`NumericValue` isn't a constraint — it's the score contributed by that option, consumed by `CalculateSelectListFieldScore` in the reporting data generator.|
|`QuestionYesNo` *(scoresheet-only)*|`YesValue`, `NoValue` (long, default 0)|Same idea — scoring weights per answer, not a constraint.|
|`DataGrid`|`Dynamic` (bool), `Columns` (`List<DataGridDefinitionColumn>` — `Name`, `Type`, `Key`), `SummaryOption` (`None`/`Above`/`Below`)|Structural definition of the grid's columns, not a value constraint.|

## Status summary

|Feature|Status|
|---|---|
|`LabelPosition` (Top/Left)|✅ Fully wired: builder toggle → stored → rendered as CSS class.|
|`LabelStyle` / `LabelCssClass`|✅ Fully wired.|
|`IsHidden` / `HideLabel` / `IsDisabled`|✅ Fully wired.|
|`Style` / `CssClass` (wrapper-level)|⚠️ Rendering supports it; **no builder UI** to set it — JSON import/export only.|
|`SecurityClassification`|⚠️ Captured with a helpful builder tooltip; **not enforced or displayed anywhere at runtime**.|
|Text / TextArea `MaxLength`, Numeric `Min`/`Max`|✅ Enforced as native HTML attributes.|
|TextArea `MinLength`|❌ Captured, never rendered.|
|Currency `Min`/`Max`|⚠️ Rendered as `data-*` attributes only, not native browser validation.|
|Date / DateTime `Min`/`Max`/`Format`|❌ Captured, never resolved or rendered anywhere.|
