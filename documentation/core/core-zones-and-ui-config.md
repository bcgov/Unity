# Core — Zones and UI Configuration

A **zone** is a configurable region of the application detail screen — a tab, or a widget within a tab. Zones are what let one tenant show a Funding Agreement tab and another hide it, and what let a program disable the Payments tab on a form that never pays.

The design's key idea: **a zone's name is a permission name**. One string simultaneously identifies a UI region, the permission that gates it, and the DOM element id.

## The model

Three plain classes in `Domain/Zones/`, serialised as JSON:

```csharp
ZoneGroupDefinition  { Name, List<ZoneTabDefinition> Tabs }
ZoneTabDefinition    { Name, DisplayName, IsEnabled, SortOrder, ElementId, List<ZoneDefinition> Zones }
ZoneDefinition       { Name, IsEnabled, SortOrder,
                       [JsonIgnore] IsConfigurationDisabled, ViewComponentType, ElementId }
```

Three properties are `[JsonIgnore]` and therefore **never persisted**: `ViewComponentType`, `ElementId` and `IsConfigurationDisabled`. They come from the code-side template every time, so the stored configuration holds only what an administrator can actually change — which zones are on, and in what order.

## The default template

`DefaultZoneDefinition.Template` is the code-side truth: one `ZoneGroupDefinition` named `ApplicationDetailsMainZone` with five tabs and fourteen zones.

```text
ApplicationDetailsMainZone
├── Review        (UnitySelector.Review.Default)
│     ├── Approval             → AssessmentApprovalViewComponent
│     ├── AssessmentResults    → AssessmentResults
│     └── AssessmentReviewList → ReviewList
├── Project       (UnitySelector.Project.Default)
│     ├── Summary  → ProjectInfoViewComponent
│     └── Location → ProjectLocationViewComponent
├── Applicant     (UnitySelector.Applicant.Default)
│     ├── Summary           → ApplicantInfoViewComponent
│     ├── Contact           → ApplicantContactInfoViewComponent
│     ├── Authority         → ApplicantSigningAuthorityViewComponent
│     ├── Location          → ApplicantPhysicalAddressViewComponent
│     └── AdditionalContact
├── Funding       (UnitySelector.Funding.Default)
│     └── Agreement → FundingAgreementInfoViewComponent
└── Payment       (UnitySelector.Payment.Default)
      ├── Summary  → PaymentInfoViewComponent   [IsConfigurationDisabled]
      └── Supplier → SupplierInfoViewComponent
```

Every `Name` is a `UnitySelector` constant. Every zone names the view component that renders it.

`IsConfigurationDisabled` marks a zone an administrator **may not turn off** — the payment summary is present whenever the Payments feature is on, because hiding it would leave the tab meaningless.

## Configuration is stored as an ABP setting

`ZoneManager` does not own a table. It serialises the whole `ZoneGroupDefinition` into one ABP setting, `GrantManager.UI.Zones`, scoped by provider:

```csharp
private const string FormProviderKey = "F";

public async Task<ZoneGroupDefinition> GetAsync(string providerName, string providerKey)
{
    var configurationJson = await _settingManager.GetOrNullAsync(
        SettingsConstants.UI.Zones, providerName, providerKey, fallback: true);
    …
    return currentConfiguration ?? DefaultZoneDefinition.Template;
}
```

| Scope | Provider | Set by |
|---|---|---|
| Per application form | `"F"` + the form id | `SetForFormAsync(formId, template)` |
| Per tenant | ABP's tenant provider | `SetForTennantAsync(template)` (note the misspelling) |

`fallback: true` gives the resolution order for free: **form → tenant → default template**. A form with no zone configuration inherits the tenant's; a tenant with none gets the code-side default. Nothing has to be seeded for the screen to work.

Deserialisation failure or a missing value both fall back to `DefaultZoneDefinition.Template` rather than throwing, so a corrupt setting degrades to the default layout rather than breaking the page.

## Reading the configuration efficiently

`GetStateSetAsync(providerName, providerKey)` flattens the whole definition into one `HashSet<string>` of enabled names:

```csharp
return zoneTemplates.Tabs
    .Where(zoneTab => zoneTab.IsEnabled)
    .SelectMany(zoneTab => new[] { zoneTab.Name }
        .Concat(zoneTab.Zones.Where(zone => zone.IsEnabled).Select(zone => zone.Name)))
    .ToHashSet();
```

Two things follow. A zone in a **disabled tab is excluded entirely**, regardless of its own `IsEnabled`. And the page renders against a single set lookup rather than walking the tree per element.

## The `<zone>` tag helper

`Web/TagHelpers/Zone/` is where the model meets the markup. `UnityZoneTagHelper` targets two elements:

```csharp
[HtmlTargetElement("zone")]
[HtmlTargetElement("zone-fieldset")]
```

`UnityZoneTagHelperService` evaluates **four independent gates** and renders the content only if all pass:

```csharp
private bool _readRequirementsSatisfied =>
    _readCondition && _featureState && _zoneState && _readPermissionState;
```

| Gate | Question |
|---|---|
| `_readCondition` | A caller-supplied boolean — arbitrary page logic |
| `_featureState` | Is the tenant feature enabled? |
| `_zoneState` | Is this zone enabled in the resolved configuration (via `IZoneChecker`)? |
| `_readPermissionState` | Does the user hold the permission? (`PermissionChecker.IsGrantedAsync`) |

Each gate answers a different question, and they fail differently: a missing **permission** hides a widget from one user; a disabled **zone** hides it from everyone on that form; a disabled **feature** hides it across the tenant.

The `zone-type` attribute (`ZoneRequirementType`) allows a zone to be **permission-only** — bypassing the zone-configuration toggle for regions that should always render when the user is allowed to see them.

Rendered output gets a `unity-zone` CSS class, and `zone-fieldset` additionally emits a legend.

### The zone debugger

The tag helper service emits a hidden diagnostic block on every zone — `alert alert-info zone-debugger-alert font-monospace d-none` — listing each requirement and whether it was satisfied, including a `ZoneRequirement` row and a link out to the form's configuration screen. Unhiding it shows, per widget, exactly which of the four gates failed. This is the fastest way to answer "why can't this user see this field?".

## Tab settings

Alongside the zone JSON, `SettingsConstants.UI.Tabs` defines per-tab settings:

```text
GrantManager.UI.Tabs             GrantManager.UI.Tabs.Submission
GrantManager.UI.Tabs.Assessment  GrantManager.UI.Tabs.Project
GrantManager.UI.Tabs.Applicant   GrantManager.UI.Tabs.Payments
GrantManager.UI.Tabs.FundingAgreement
```

These predate the zone system and cover tabs the zone template does not, notably Submission.

## Where zones are configured

- **Form configuration** (`Web/Pages/FormConfiguration/`) — the per-form zone editor, writing through `SetForFormAsync`.
- **Application UI settings** (`Web/Components/ApplicationUiSettingGroup/`, `Web/Settings/ApplicationUiSettingPageContributor.cs`) — the tenant-level defaults, contributed into ABP's setting-management page.

## Related: Flex custom fields

Zones control which **built-in** widgets appear. Flex worksheets add **custom fields** into named UI anchors on the same screen — a different mechanism with a similar effect. `FlexConsts.PaymentInfoUiAnchor` is one such anchor, rendered inside the Payment Summary zone. See [`flex/flex-web-ui.md`](../flex/flex-web-ui.md).

A field can therefore be hidden three ways: its zone is disabled, its permission is not granted, or its Flex worksheet is not assigned to the form.
