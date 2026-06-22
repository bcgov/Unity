# Runtime Prompt Templates

Runtime prompts are now resolved from the database-backed `AIPrompts` and `AIPromptVersions` records seeded by the AI module.
These files are retained as prompt asset references and seed inputs, not as the runtime source of truth.

Current prompt asset references:

- `application-analysis.system.txt`
- `application-analysis.user.txt`
- `application-analysis.rubric.txt` (optional, when `{{RUBRIC}}` is used)
- `application-analysis.score.txt` (optional, when `{{SCORE}}` is used)
- `application-analysis.output.txt` (optional, when `{{OUTPUT}}` is used)
- `application-analysis.rules.txt` (optional, when `{{RULES}}` is used)
- `common.*.txt` (optional shared fragments for `{{COMMON_*}}` placeholders)
- `attachment-summary.system.txt`
- `attachment-summary.user.txt`
- `attachment-summary.output.txt` (optional, when `{{OUTPUT}}` is used)
- `attachment-summary.rules.txt` (optional, when `{{RULES}}` is used)
- `application-scoring.system.txt`
- `application-scoring.user.txt`
- `application-scoring.output.txt` (optional, when `{{OUTPUT}}` is used)
- `application-scoring.rules.txt` (optional, when `{{RULES}}` is used)

Placeholders:

- `{{SCHEMA}}`
- `{{DATA}}`
- `{{ATTACHMENTS}}`
- `{{RUBRIC}}`
- `{{SCORE}}`
- `{{OUTPUT}}`
- `{{RULES}}`
- `{{ATTACHMENT}}`
- `{{DATA}}`
- `{{ATTACHMENTS}}`
- `{{SECTION}}`
- `{{RESPONSE}}`

Version selection:

- Required: `Azure:Operations:Defaults:PromptVersion = v0|v1`, with optional overrides under `Azure:Operations:<Operation>:PromptVersion`.
- Unknown or missing version values fail at runtime.

Template loading is strict:

- Core prompt records are required for each version.
- Missing required prompt records fail fast at runtime with a configuration error.
- Runtime prompt rendering resolves placeholders from the stored template text plus the version metadata sections.
