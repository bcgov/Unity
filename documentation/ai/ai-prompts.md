# AI Prompts

Prompts are data, not code. Every model call resolves an `AIPrompt` row at runtime, renders its user template against the operation's inputs, and sends the pair. This document covers how a prompt is chosen, how it is rendered, and how prompts are administered.

## Prompt families and versions

`AIPrompt.Name` is the **family**, and it is always equal to an `AIOperation.Name` — the six values in `AIPromptTypes`:

```text
AttachmentSummary · ApplicationAnalysis · ApplicationScoring
FormMapping · FormWorksheet · FormScoresheet
```

`AIPrompt.VersionNumber` selects a variant within the family, and `(TenantId, Name, VersionNumber)` is uniquely indexed. Versions are ordinary integers rendered as `v{n}` in the settings record (`PromptVersion = $"v{prompt.VersionNumber}"`).

`AIPromptDataSeeder` seeds global prompts (`TenantId == null`):

| Family | Seeded versions | Metadata seeded |
|---|---|---|
| `ApplicationAnalysis` | v0, v1, v2 | — |
| `AttachmentSummary` | v0, v1, v2 | — |
| `ApplicationScoring` | v0, v1, v2 | — |
| `FormMapping` | v2 | yes |
| `FormWorksheet` | v2 | yes |
| `FormScoresheet` | v2 | yes |

The three application-level families keep their older versions on purpose, so a request can pin `v0` or `v1` and reproduce an older result. The form-generation families start at v2.

The seeder is 973 lines, almost all of it prompt text held in `const string` fields — see [ai-roadmap.md](ai-roadmap.md#prompt-text-lives-in-a-973-line-c-file).

## How a prompt is chosen

Two code paths resolve prompts, and they differ.

### Which version is active — `OpenAIConfigurationResolver.ResolvePromptAsync`

Used when building operation settings, this ignores version and asks "what is the newest active prompt for this family, for this tenant?":

```csharp
using (multiTenantDataFilter.Disable())
{
    var prompts = await promptRepository.GetListAsync(p => p.Name == promptFamily && p.IsActive);

    var selected = currentTenant.Id is Guid tenantId
        ? prompts.Where(p => p.TenantId == tenantId).OrderByDescending(p => p.VersionNumber).FirstOrDefault()
        : null;

    selected ??= prompts.Where(p => p.TenantId == null).OrderByDescending(p => p.VersionNumber).FirstOrDefault();

    return selected ?? throw new InvalidOperationException(
        $"AI prompt family '{promptFamily}' has no active prompt for the current tenant.");
}
```

**A tenant's own prompt wins outright.** If the tenant has any active prompt in the family, the global rows are not considered at all — including when the global family has a higher version number.

### Which row to load — `AIPromptTemplateStore.GetRequiredPromptAsync`

Used to fetch the actual template, given a family *and* a version. Same tenant-first preference, but pinned to one version:

```csharp
var prompts = await promptRepository.GetListAsync(
    p => p.Name == promptType && p.VersionNumber == versionNumber && p.IsActive);

var prompt = currentTenant.Id is Guid tenantId ? prompts.FirstOrDefault(p => p.TenantId == tenantId) : null;
prompt ??= prompts.FirstOrDefault(p => p.TenantId == null);

if (prompt == null || !prompt.IsActive)
    throw new InvalidOperationException($"AI prompt '{promptType}' version '{normalizedPromptVersion}' is not configured.");
```

The version comes from the caller if one was pinned, otherwise from the settings record:

```csharp
request.PromptVersion ?? settings.PromptVersion
```

`AIGenerationSubmissionDto.PromptVersion` flows from the API, through the queue, into the job args, and down to here — so a request can pin a version end to end. `OpenAIPromptRenderer.ResolvePromptVersion` / `ResolvePromptVersionNumber` normalise the `"v2"` form to the integer.

Both paths **disable the multi-tenancy data filter** and re-apply tenancy by hand, because `AIPrompts` lives in the host database. See [ai-domain-model.md](ai-domain-model.md#two-ai-schemas-in-two-databases).

## Rendering

`AIPromptTemplateRenderer` renders the **user** prompt. The system prompt is sent verbatim.

### Placeholders

A placeholder is `{{TOKEN}}`. Each operation supplies its own set:

| Builder | Placeholders supplied |
|---|---|
| `BuildApplicationAnalysisUserPrompt` | `SCHEMA`, `DATA`, `ATTACHMENTS` |
| `BuildAttachmentSummaryUserPrompt` | `ATTACHMENT`, `ATTACHMENTS` (both bound to the same value) |
| `BuildApplicationScoringUserPrompt` | `DATA`, `ATTACHMENTS`, `SECTION`, `RESPONSE` |
| `BuildFormMappingUserPrompt` | `DATA` — also used for form worksheet and form scoresheet |

### Metadata sections become placeholders

`AIPrompt.MetadataJson` is parsed into named sections, each of which is added as a placeholder if the runtime did not already supply one (`TryAdd`, so runtime values always win). This is how the form-generation prompts keep large reusable blocks — rules, output schemas, examples — out of the user template while still being tenant-editable.

### `RESPONSE` and `OUTPUT` are aliases

```csharp
if (!replacements.ContainsKey("RESPONSE") && replacements.TryGetValue("OUTPUT", out var outputTemplate))
    replacements["RESPONSE"] = outputTemplate;
else if (!replacements.ContainsKey("OUTPUT") && replacements.TryGetValue("RESPONSE", out var responseTemplate))
    replacements["OUTPUT"] = responseTemplate;
```

A template may use either name for the expected-output block.

### Unresolved placeholders throw

Rendering scans the template for placeholders first, then fails loudly rather than sending a prompt with a literal `{{FOO}}` in it:

```csharp
throw new InvalidOperationException($"Unresolved prompt placeholders: {string.Join(", ", unresolved)}");
```

There is a matching guard for malformed placeholder syntax — `Invalid prompt placeholders: ...`. Both are `InvalidOperationException`, which the transport maps to `PermanentFailure`, so a broken prompt fails immediately instead of burning three attempts.

**This is the main risk when editing a prompt through the UI**: adding `{{SOMETHING}}` that no builder supplies and no metadata section defines breaks that operation for that tenant at the next run, with the failure appearing on the generation request rather than at save time.

## Scoring prompts get an extra layer

Application scoring does more than substitute. `OpenAIPromptRenderer` builds the section block and a response template from the scoresheet schema, and **aliases the question ids**:

```csharp
var section  = OpenAIPromptRenderer.BuildAliasedApplicationScoringSection(
                   request.SectionName, sectionJson, out var questionIdAliasMap);
var response = OpenAIPromptRenderer.BuildApplicationScoringResponseTemplate(section);
```

Raw question ids are GUIDs; the aliasing replaces them with short stable tokens for the model and keeps a map to translate the answers back. `OpenAIResponseParser.ParseApplicationScoringResponse(content, questionIdAliasMap)` reverses it.

If the response template comes back as `"{}"` — meaning the section schema produced nothing usable — the operation logs a warning and returns an empty response **without calling the model at all**.

The section payload itself is built by `AIApplicationInputBuilder.BuildSectionQuestionsData`, which projects each scoresheet field to:

```json
{ "id": "...", "section": "...", "question": "...", "description": "...",
  "type": "SelectList", "options": [{ "number": 1, "value": "..." }],
  "allowed_answers": ["1", "2", "3"] }
```

`options` and `allowed_answers` are populated only for `SelectList` questions, from the Flex `QuestionSelectListDefinition`. That is what constrains the model to answer with a valid option rather than free text.

## Administering prompts

### The Prompts page

`~/Prompts` (`Pages/Prompts/Index.cshtml` + `Index.js`, ~370 lines) lists prompt families and their versions, with create/edit modals for both a prompt and an individual version entry:

| Page | Purpose |
|---|---|
| `Prompts/Index` | The list |
| `Prompts/CreateModal`, `Prompts/EditModal` | Prompt-level create/edit |
| `Prompts/Entries/CreateEntryModal`, `Entries/EditEntryModal` | Version-level create/edit |

The menu item is added by `AIMenuContributor` with `.OnlyWhenInRole(IdentityConsts.ITOperationsRoleName)`, and is **not** added at all when the Onboarding specialization is enabled.

### `AIPromptAppService`

`api/app/ai/prompts`, a `CrudAppService<AIPrompt, AIPromptDto, Guid, PagedAndSortedResultRequestDto, CreateUpdateAIPromptDto>` with **every** policy — get, list, create, update, delete — set to `IdentityConsts.ITOperationsPolicyName`.

Every method disables the multi-tenancy filter and enforces tenancy manually:

```csharp
private void EnsureReadAccess(AIPrompt prompt)
{
    if (_currentTenant.Id is Guid tenantId && prompt.TenantId is not null && prompt.TenantId != tenantId)
        throw new AbpAuthorizationException("The selected AI prompt belongs to another tenant.");
}

private void EnsureMutationAccess(AIPrompt prompt)
{
    if (_currentTenant.Id is Guid tenantId && prompt.TenantId != tenantId)
        throw new AbpAuthorizationException("The selected AI prompt cannot be modified from this tenant.");
}
```

The asymmetry is deliberate: a tenant may **read** global prompts (`TenantId is null` passes the read check) but may not **modify** them (`prompt.TenantId != tenantId` fails the mutation check for a global row).

| Method | Behaviour |
|---|---|
| `GetListAsync` | Returns global prompts plus the current tenant's, sorted by name then descending version. Loads everything and pages in memory — see [ai-roadmap.md](ai-roadmap.md#the-prompt-list-pages-in-memory) |
| `GetAsync` | Read-access checked |
| `GetByPromptAsync(promptId)` | All versions of the family that the given prompt belongs to, ascending by version |
| `CreateAsync` | Creates a **new version** of an existing prompt family. Takes `input.PromptId` to identify the family, targets `_currentTenant.Id ?? prompt.TenantId`, and rejects a duplicate `(tenant, name, version)` with a `UserFriendlyException` |
| `UpdateAsync` | Edits version number, system prompt, user prompt, metadata and active flag; rejects a version collision with another row |
| `DeleteAsync` | `[RemoteService(false)]` — not exposed over HTTP; mutation access checked |

Note what `CreateAsync` does **not** do: there is no way through this service to create a brand-new prompt *family*. Families come from the seeder, and the UI creates new versions of them. A tenant that edits a prompt is creating a tenant-scoped version that then wins over every global version in that family.

Ids are created with `Guid.CreateVersion7()` rather than ABP's `GuidGenerator` — see [ai-roadmap.md](ai-roadmap.md#convention-drift).

## Practical notes for editing a prompt

- **Activating a tenant version overrides the whole family.** Once a tenant has any active prompt for `ApplicationScoring`, global improvements to that family stop reaching them.
- **Changes take up to five minutes to apply**, and independently per pod, because the resolved settings (including the active prompt version) sit in `IMemoryCache`.
- **Deactivating every version of a family breaks the operation** — `ResolvePromptAsync` throws `"has no active prompt for the current tenant"`, which surfaces as a failed generation request.
- **Placeholders are validated at render time, not save time.** Test a new prompt by running the operation.
- To reproduce a historical result, pin the version: pass `promptVersion` on the generate call.
