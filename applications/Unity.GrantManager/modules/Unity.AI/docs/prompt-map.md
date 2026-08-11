# Prompt Map

## Prompt families
- `ApplicationAnalysis` - review and recommendation
- `AttachmentSummary` - attachment summary
- `ApplicationScoring` - question scoring
- `FormMapping` - CHEFS to Unity mapping
- `FormWorksheet` - worksheet generation

## Versions
- `v0`, `v1`, `v2` live under `Runtime/Prompts/Versions`
- The seeder loads built-in prompt rows from those versions
- Runtime selects the newest active prompt by family.

## Tenant selection
- `AIOperation.Name` is the prompt family; operations do not pin a prompt row or version.
- Host requests use the newest active global prompt in the family.
- Tenant requests use the newest active prompt owned by that tenant, falling back to the newest active global prompt.
- To roll back a tenant or global prompt, deactivate the active version and leave the prior version active.
- Tenant prompt rows are administrator-created; deployments seed only global prompts and operations.

## Prompt rules
- Versioned prompts are the source of truth.
- Prompt templates define the request shape.
- Structured outputs should stay JSON-shaped.
- New versions should not silently change behavior.

## Build Rule
Use [`implementation-playbook.md`](./implementation-playbook.md) when adding a new prompt-backed operation.
