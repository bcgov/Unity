# Unity.AI Module Documentation

Unity.AI is Unity Portal's **generative AI** module. It runs six named operations against an Azure-hosted OpenAI deployment, all of which take grant data in and produce structured JSON out:

| Operation | Produces |
|---|---|
| **Attachment summary** | A plain-language summary of each uploaded document |
| **Application analysis** | Structured findings about a submission |
| **Application scoring** | Draft answers for every question on the form's scoresheet |
| **Form mapping** | Suggested mappings from CHEFS form fields to Unity fields |
| **Form worksheet** | A draft Flex worksheet generated from a form schema |
| **Form scoresheet** | A draft Flex scoresheet generated from a form schema |

Nothing runs synchronously. Every operation is queued as a background job behind a distributed lock, a per-user rate limit, and a three-way gate of **feature × tenant setting × permission**. Every result is a draft that a human accepts, edits, or discards — the module never commits AI output as final.

This folder documents how the module is built and how it is used. Read in this order:

1. **[ai-overview.md](ai-overview.md)** — what problem it solves, the four architectural facts, the module layout, the gating model, external dependencies, core concepts glossary.
2. **[ai-domain-model.md](ai-domain-model.md)** — `AIModel`, `AIOperation`, `AIPrompt`, the host-side request and review entities, the two `AI` schemas, seeders, permissions and settings.
3. **[ai-generation-pipeline.md](ai-generation-pipeline.md)** — submit → validate → lock → rate-limit → enqueue → execute → persist, plus status reporting and the automatic-intake trigger.
4. **[ai-provider-runtime.md](ai-provider-runtime.md)** — Azure OpenAI configuration resolution, the transport, the retry-and-validate loop, the outcome taxonomy, and prompt logging.
5. **[ai-prompts.md](ai-prompts.md)** — prompt families and versioning, tenant overrides, the placeholder renderer, prompt metadata, and the prompts admin UI.
6. **[ai-operations.md](ai-operations.md)** — the six operations in detail: what goes in, what comes back, where the result lands, and the form-generation review workflow.
7. **[ai-web-ui.md](ai-web-ui.md)** — pages, widgets, menus, the settings group, the legal disclaimer, and permission gating on each surface.
8. **[ai-roadmap.md](ai-roadmap.md)** — known rough edges: hard-coded provider, per-pod caches, swallowed failures, and other gaps worth knowing before extending this module.

## Source location

```
applications/Unity.GrantManager/modules/Unity.AI/
├── src/
│   ├── Unity.AI.Domain.Shared/          feature consts, settings keys, PromptType,
│   │                                      AIResource localization + AILocalizationKeys
│   ├── Unity.AI.Application.Contracts/   DTOs, app service interfaces, operation definitions,
│   │                                      requests/responses, permissions, provider-facing abstractions
│   ├── Unity.AI.Application/             Domain/          AIModel, AIOperation, AIPrompt
│   │                                     EntityFrameworkCore/  ConfigureAI() model extension
│   │                                     Runtime/Execution/    Azure OpenAI client, transport,
│   │                                                           config + prompt resolution, validators
│   │                                     Operations/      per-operation services + execution strategy
│   │                                     Generation/      AIGenerationAppService
│   │                                     RateLimit/, Prompts/, Settings/, Extraction/, DataSeed/
│   └── Unity.AI.Web/                     Prompts pages, AIReporting page, settings group,
│                                           AIConfiguration widget, legal disclaimer
```

Note the layout: like `Unity.Payments` and unlike `Unity.Flex`, this module has **no separate `.Domain` or `.EntityFrameworkCore` projects** — the entities live in `Unity.AI.Application/Domain/` and the model configuration in `Unity.AI.Application/EntityFrameworkCore/`. There is a `Unity.AI.Domain.Shared` project, but its csproj is named `Unity.AI.Shared.csproj`.

## The host owns the orchestration

This module defines *how* to call the model. The host defines *when*, *with what*, and *where the answer goes*. That half lives in `applications/Unity.GrantManager/src/Unity.GrantManager.Application/GrantApplications/Automation/`:

| Host component | Role |
|---|---|
| `Generation/ApplicationAIGenerationQueue.cs` | The real `IApplicationGenerationQueue` — dedupe lock, prerequisite validation, rate limit, enqueue |
| `Generation/BackgroundJobs/AIGenerationBackgroundJob.cs` | Owns tenant scope and the request lifecycle (Queued → Running → Completed/Failed) |
| `Generation/BackgroundJobs/AIGenerationOperationExecutorRegistry.cs` | Resolves the `IAIGenerationOperationExecutor` for an operation type |
| `Operations/*/​*OperationExecutor.cs` | Six executors — build the input, call the module, persist the result |
| `Operations/AIApplicationInputDataProvider.cs` | Supplies applications, submissions, form versions, scoresheets and attachment summaries |
| `Operations/ChefsFileAttachmentStreamProvider.cs` | Opens attachment content streams from CHEFS |
| `AIGenerationPrerequisiteValidator.cs` | Per-operation "can this even run?" checks |
| `AIGenerationStatusReader.cs`, `Generation/AIGenerationActivityProvider.cs` | Status and "is this user already generating?" for the rate limiter |
| `Generation/Handlers/QueueApplicationAIPipelineOnProcessHandler.cs` | Automatic pipeline on application intake |
| `Generation/Handlers/CreateAIAssessmentOnScoringGeneratedHandler.cs` | Turns generated scoring into an AI assessment |
| `src/Unity.GrantManager.Domain/GrantApplications/AIGenerationRequest.cs` | The queued-request entity (`AI.AIRequests`, **host** DB) |
| `src/Unity.GrantManager.Domain/Applications/ApplicationScoresheetAnswers.cs` | AI scoresheet answers (`AI.ApplicationScoresheetAnswers`, **tenant** DB) |
| `src/Unity.GrantManager.Domain/ApplicationForms/GenerationReview.cs` | Draft review state for form-generation operations (`AI.GenerationReviews`, **tenant** DB) |

The module also depends on `Unity.Flex` (`FlexApplicationModule`) for scoresheet and worksheet types.

A related one-page visual summary is in `documentation/handover/ai-module-handover.html`.
