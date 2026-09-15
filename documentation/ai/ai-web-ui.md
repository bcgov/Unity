# AI Web UI

The module's own web project is small — a prompts admin area, an embedded reporting page, a settings group and one widget. The surfaces staff use most (the AI Analysis tab, the generate buttons, the attachment summaries) are **host-side**, in `src/Unity.GrantManager.Web/`.

`AIWebModule` registers the menu contributor, the embedded file set, the Mapperly mapper, and an `AISettingPageContributor` on ABP's setting-management page options.

## Menu

`AIMenuContributor` adds up to two main-menu items, on different gates.

### AI Prompts

```csharp
if (!await specializationChecker.IsEnabledAsync(SpecializationConsts.Onboarding))
{
    await context.AddItemAsync(new ApplicationMenuItem(
        AIMenus.Prompts, "AI Prompts", "~/Prompts", icon: "fl fl-ai-prompts", order: 900)
        .OnlyWhenInRole(IdentityConsts.ITOperationsRoleName));
}
```

Two conditions, both unusual: it is hidden entirely when the **Onboarding specialization** is enabled, and it is gated by the **`ITOperations` role** rather than by any AI permission or feature. The display name is a hard-coded string, not a localized resource.

### AI Reporting

```csharp
var reportingEnabled = await featureChecker.IsEnabledAsync("Unity.AIReporting")
    && await settingProvider.GetAsync<bool>(AISettings.ReportingEnabled, defaultValue: false);

if (reportingEnabled || isItAdmin) { /* add the item */ }
```

Feature **and** setting, **or** the `ITAdmin` role as an override. The item itself also carries `requiredPermissionName: AIPermissions.Reporting.ReportingDefault`, so an IT admin without that permission sees nothing — the role override only bypasses the feature/setting pair.

## Pages

| Route | Page model | Purpose |
|---|---|---|
| `~/Prompts` | `Pages/Prompts/Index.cshtml(.cs)` + `Index.js` | Prompt families and versions |
| `~/Prompts` modals | `Prompts/CreateModal`, `Prompts/EditModal` | Prompt-level create/edit |
| `~/Prompts` entry modals | `Prompts/Entries/CreateEntryModal`, `Entries/EditEntryModal` | Version-level create/edit |
| `~/AIReporting` | `Pages/AIReporting/Index.cshtml(.cs)` + `Index.js` | Embedded AI reporting host |
| `Settings/LegalDisclaimerModal` | `LegalDisclaimerModalModel` | The disclaimer shown before AI is switched on |

### AI Reporting

`IndexModel.OnGetAsync` recomputes the same gate as the menu:

```csharp
var isItAdmin = (await authorizationService.AuthorizeAsync(User, IdentityConsts.ITAdminPolicyName)).Succeeded;
var featureAndSettingEnabled = await featureChecker.IsEnabledAsync("Unity.AIReporting")
    && await settingProvider.GetAsync<bool>(AISettings.ReportingEnabled, defaultValue: false);

CanViewAiReporting = featureAndSettingEnabled || isItAdmin;
```

If permitted, it resolves the `REPORTING_AI` dynamic URL through the host's `IEndpointManagementAppService` and embeds it. A missing endpoint is caught, logged as a warning, and leaves `ReportingAiUrl` empty rather than throwing — the page renders with nothing in it.

### Prompts

`Pages/Prompts/Index.js` (~370 lines) drives the list and the four modals against `api/app/ai/prompts`. Behaviour worth knowing before using it is in [ai-prompts.md](ai-prompts.md#administering-prompts) — in particular that saving a prompt as a tenant creates a **tenant-scoped version that overrides the whole global family**, and that placeholder errors surface at generation time rather than at save.

## Settings group

`AISettingPageContributor` adds an **AI Configuration** group to ABP's setting-management page:

```csharp
RequiredFeatures(SettingManagementFeatures.Enable);
RequiredPermissions(AIPermissions.Configuration.ConfigureAI);
context.Groups.Add(new SettingPageGroup("AI.Configuration", "AI Configuration", typeof(AISettingViewComponent), order: 5));
```

`AISettingViewComponent` renders the three tenant toggles — automatic generation, manual generation, reporting — reading current values through `ISettingProvider` and saving through `AIConfigurationAppService` (`PUT api/app/ai/configuration/tenant`), which requires the same `SettingManagement.ConfigureAI` permission on both the read and the write.

## The legal disclaimer

`Views/Shared/Scripts/AiLegalDisclaimer.js` exposes one function:

```javascript
unity.aI.legalDisclaimer.confirmIfNeeded(turningOn, onConfirmed)
```

Turning a toggle **off** proceeds immediately. Turning one **on** opens `Settings/LegalDisclaimerModal` and only calls `onConfirmed` when the modal resolves. The script is bundled into both the AI settings group and the `AIConfiguration` widget, so the disclaimer is acknowledged wherever AI is enabled — at tenant level or per form.

`LegalDisclaimerModalModel` is a bare page model whose `OnPost` returns `NoContent()`; acknowledgement is a UI gate, not a stored consent record.

## The AIConfiguration widget

`Views/Shared/Components/AIConfiguration/` — an ABP `[Widget]` shown on the application form configuration screen:

```csharp
ShowAutomatic = await settingProvider.GetAsync<bool>(AISettings.AutomaticGenerationEnabled, false);
ShowManual    = await settingProvider.GetAsync<bool>(AISettings.ManualGenerationEnabled, false);
AutomaticallyGenerateAIAnalysis = applicationForm.AutomaticallyGenerateAIAnalysis;
ManuallyInitiateAIAnalysis      = applicationForm.ManuallyInitiateAIAnalysis;
```

Each per-form toggle is only rendered when the corresponding **tenant setting** is on — so a program cannot opt into automatic generation that the tenant has not enabled. Its script bundle pulls in the legal disclaimer.

There is also an `AIPromptsWidget` view component (`Views/Shared/Components/AIPromptsWidget/`) with a `Default.cshtml`.

## Host-side surfaces

The screens users actually generate from are in the host, mostly on the application detail page (`src/Unity.GrantManager.Web/Pages/GrantApplications/Details.cshtml`):

| Asset | Role |
|---|---|
| `ai-analysis.js` | The AI Analysis tab — renders findings, triggers regeneration |
| `ai-generation-api.js` | Calls `api/app/ai/generation/*` |
| `ai-generation-button-state.js` | Enables/disables generate buttons from generation status |
| `ai-rate-limit.js` | Polls `AIRateLimiter.GetStateAsync` and reflects the cooldown in the button |
| `ai-generation-button.css` | Button and cooldown styling |

The tab and the Generate button are gated separately in the Razor page — a **view guard** (feature + view permission, controlling whether the tab and existing results appear at all) and a **generate guard** (feature + generate permission + the form's `ManuallyInitiateAIAnalysis` flag, controlling only the button). Someone can therefore read an AI analysis generated automatically at intake without being able to regenerate it.

Generate buttons render disabled with `data-ai-cooldown-checking="1"` and are enabled once the rate-limit state comes back — so the default state is "not clickable" until the client confirms the user is not in a cooldown.

Other host surfaces that consume AI output:

- **Form mapping screen** (`Pages/ApplicationForms/Mapping.cshtml`) — the form-mapping review workflow.
- **Assessment scores widget** and **review list** — where the AI assessment produced from scoring appears alongside human assessments.
- **CHEFS attachments component** — where attachment summaries are surfaced.

## Permission gating summary

| Surface | Gate |
|---|---|
| AI Prompts menu + pages | `ITOperations` role; hidden under the Onboarding specialization |
| AI Reporting menu + page | (`Unity.AIReporting` feature **and** `ReportingEnabled` setting) **or** `ITAdmin` role; menu item additionally needs `AI.Reporting` |
| AI settings group | `SettingManagement.ConfigureAI` (visible when any AI feature is on) |
| AIConfiguration widget toggles | Each toggle needs its matching tenant setting to be on |
| AI Analysis tab and results | Feature + `AI.ViewApplicationAnalysis` |
| Generate buttons | Feature + `AI.Generate*` + the form's `ManuallyInitiateAIAnalysis` |
| Every generation endpoint | Feature (via `AIFeatureGuard`) + `AI.Generate*` (checked twice — attribute and `SubmitAsync`) |
| Generation status endpoint | `AI.View*` for the requested operation |
