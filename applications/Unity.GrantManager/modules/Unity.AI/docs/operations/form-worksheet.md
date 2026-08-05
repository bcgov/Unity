# Form Worksheet

## Goal
Generate a recommended worksheet definition for a form version.

## Inputs
- Form version context
- Form name
- Existing worksheet links
- Worksheet field context

## Rule
- Prefer existing Unity core fields when they already fit the need.
- Only add new worksheet fields when the form genuinely needs extra Unity fields.

## Surface
- `POST /api/app/ai/generation/form-worksheet`
- `GET /api/app/ai/generation/status`

## Contract
- Structured Flex worksheet JSON output. Returns an immediate queued result via API app service, queue, background job, and AI runtime.

## Output Shape
- Full worksheet definition JSON.
- Include only additional worksheet fields that the form needs beyond core Unity fields.
- Keep the result valid JSON and compatible with Flex import.

## Notes
- The AI output should stay valid JSON.
