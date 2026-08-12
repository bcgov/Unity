# Prompt Map

## Prompt families
- `ApplicationAnalysis` - review and recommendation
- `AttachmentSummary` - attachment summary
- `ApplicationScoring` - question scoring
- `FormMapping` - CHEFS to Unity mapping
- `FormWorksheet` - worksheet generation
- `FormScoresheet` - scoresheet generation

## Versions
- Built-in prompt rows are defined and seeded by `AIPromptDataSeeder`.
- Families may have `v0`, `v1`, and `v2` rows; a new operation only needs the versions it supports.
- Without an explicit request version, runtime selects the newest active prompt by family.

## Tenant selection
- `AIOperation.Name` is the prompt family; operations do not pin a prompt row or version.
- An explicit request version selects that active version, with the same tenant/global fallback.
- Otherwise, host requests use the newest active global prompt in the family.
- Otherwise, tenant requests use the newest active prompt owned by that tenant, falling back to the newest active global prompt.
- To roll back a tenant or global prompt, deactivate the active version and leave the prior version active.
- Tenant prompt rows are administrator-created; deployments seed only global prompts and operations.

## Prompt rules
- Versioned prompts are the source of truth.
- Prompt templates define the request shape.
- Structured outputs should stay JSON-shaped.
- A new version should be additive and must not silently change an active prompt's behavior.

## Build Rule
Use [`implementation-playbook.md`](./implementation-playbook.md) when adding a new prompt-backed operation.
