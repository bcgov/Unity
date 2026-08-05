# Form Mapping

## Goal
Generate recommended CHEFS-to-Unity field mapping for a form version.

## Inputs
- CHEFS fields from the form version
- Unity core intake fields
- Worksheet-derived custom fields when available

## Rule
- Prefer existing Unity core intake fields where they already fit the source field.
- Only suggest worksheet fields or worksheet creation when the form genuinely needs them.

## Surface
- `POST /api/app/ai/generation/form-mapping`
- `GET /api/app/ai/generation/status`
- `GET /api/app/application-form-version/{id}`

## Contract
- Structured mapping recommendation JSON output. Returns an immediate queued result via API app service, queue, background job, and AI runtime.

## Output Shape
- Core field matches.
- Worksheet field matches.
- Worksheet creation suggestions.
- Issues or conflicts.
- Keep the result valid JSON and compatible with the mapping page flow.

## Notes
- The AI response is expected to stay structured and JSON-shaped.
- For new operations, follow [`implementation-playbook.md`](../implementation-playbook.md).
