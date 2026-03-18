# Runtime Prompt Templates

These files are the source of truth for runtime prompts.
`OpenAIRuntimeService` resolves templates from:

- `AI/Prompts/Versions/<version>/<template>.txt`

Current templates:

- `analysis.system.txt`
- `analysis.user.txt`
- `analysis.rubric.txt` (optional, when `{{RUBRIC}}` is used)
- `analysis.score.txt` (optional, when `{{SCORE}}` is used)
- `analysis.output.txt` (optional, when `{{OUTPUT}}` is used)
- `analysis.rules.txt` (optional, when `{{RULES}}` is used)
- `common.*.txt` (optional shared fragments for `{{COMMON_*}}` placeholders)
- `attachment.system.txt`
- `attachment.user.txt`
- `attachment.output.txt` (optional, when `{{OUTPUT}}` is used)
- `attachment.rules.txt` (optional, when `{{RULES}}` is used)
- `scoresheet.system.txt`
- `scoresheet.user.txt`
- `scoresheet.output.txt` (optional, when `{{OUTPUT}}` is used)
- `scoresheet.rules.txt` (optional, when `{{RULES}}` is used)

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

- Preferred: `Azure:Operations:Defaults:PromptVersion = v0|v1`, with optional overrides under `Azure:Operations:<Operation>:PromptVersion`
- Legacy fallback: `Azure:OpenAI:PromptVersion = v0|v1`
- Unknown or missing version defaults to `v1`.

Template loading is strict:

- Core templates are required for each version.
- Missing required templates fail fast at runtime with a configuration error.
- Fragment templates are required when the corresponding placeholder is present in the parent template.
- Fragment resolution is automatic using `<base>.<placeholder-lower>.txt` from the same version folder.
  - Example: `analysis.user.txt` with `{{RULES}}` resolves `analysis.rules.txt`.
- `{{COMMON_*}}` placeholders resolve to `common.<suffix>.txt` where suffix is lower-cased and `_` becomes `.`.
  - Example: `{{COMMON_RULES}}` resolves `common.rules.txt`.
