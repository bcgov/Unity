# Unity.AI Index

## Domain.Shared
AI constants:
- feature flags
- permission names
- localization keys
- prompt type names

## Application.Contracts
Public AI surface:
- app service interfaces
- queue interfaces
- DTOs
- permission definitions

## Application
AI implementation:
- runtime
- prompt seeding
- generation app services
- validators
- prompt logging

## Web
UI-facing AI bits:
- menus
- generation buttons
- status polling

## Files
### Application
- `AI/Operations` - validators and helpers
- `AI/Runtime` - rendering, parsing, logging, provider calls
- `AI/Prompts` - prompt types and template plumbing
- `DataSeed` - seeded prompt and operation data
- `Generation/AIGenerationAppService.cs` - generation API

### Application.Contracts
- `AI/IAIService.cs` - runtime contract
- `Generation/IAIGenerationAppService.cs` - generation app service contract
- `Generation/*ResultDto.cs` - queued result DTOs
- `AI/Operations/IAIGenerationPrerequisiteValidator.cs` - queue prerequisites
- `Automation/IApplicationAIGenerationQueue.cs` - queue contract
- `Permissions/*` - permissions

### Domain.Shared
- `Features/AIFeatures.cs` - feature flags
- `Localization/AILocalizationKeys.cs` - messages
- `PromptTypes/AIPromptTypes.cs` - prompt family names

### Web
- `Menus/AIMenuContributor.cs` - menu entries
- `Menus/AIMenus.cs` - menu item names

## Access
| Operation | View | Generate |
| --- | --- | --- |
| Application Analysis | `ViewApplicationAnalysis` | `GenerateApplicationAnalysis` |
| Attachment Summary | `ViewAttachmentSummary` | `GenerateAttachmentSummaries` |
| Application Scoring | `ViewScoringResult` | `GenerateScoring` |
| Form Mapping | `ViewFormMapping` | `GenerateFormMapping` |
| Form Worksheet | `ViewFormWorksheet` | `GenerateFormWorksheet` |
| Form Scoresheet | `ViewFormScoresheet` | `GenerateFormScoresheet` |

- Features:
  - `Unity.AI.ApplicationAnalysis`
  - `Unity.AI.AttachmentSummaries`
  - `Unity.AI.Scoring`
  - `Unity.AI.FormMapping`
  - `Unity.AI.FormWorksheet`
  - `Unity.AI.FormScoresheet`

- Rule:
  - Both permission and feature gate must allow generation.

## AI Notes
- Prompt logging: logs rendered system/user prompts and provider output.
- Response parsing: parses provider output into stable app-facing results.
- Feature gating: disabled features fail early at the API boundary.
- Background jobs: mark failures, then re-throw.
- New operation playbook: see `implementation-playbook.md`.
