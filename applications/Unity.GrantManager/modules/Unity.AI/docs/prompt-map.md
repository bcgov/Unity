# Prompt Map

## Prompt families
- `ApplicationAnalysis` - review and recommendation
- `AttachmentSummary` - attachment summary
- `ApplicationScoring` - question scoring
- `FormMapping` - CHEFS to Unity mapping
- `FormWorksheet` - worksheet generation
- `FormScoresheet` - scoresheet generation

## Versions
- `v0`, `v1`, `v2` live under `AI/Prompts/Versions`
- The seeder loads built-in prompt rows from those versions
- Runtime selects by prompt family and version

## Prompt rules
- Versioned prompts are the source of truth.
- Prompt templates define the request shape.
- Structured outputs should stay JSON-shaped.
- New versions should not silently change behavior.

## Build Rule
Use [`implementation-playbook.md`](./implementation-playbook.md) when adding a new prompt-backed operation.
