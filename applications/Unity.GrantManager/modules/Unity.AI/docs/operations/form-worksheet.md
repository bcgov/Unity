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
- Structured worksheet field-suggestion JSON. The POST request enqueues generation and returns without the generated payload; clients use the shared status endpoint while the background executor validates the suggestions and creates an unpublished worksheet for review.

## Output Shape
- A `fields` collection containing the suggested additional worksheet fields.
- Include all applicable additional fields in the collection; do not limit the response to one suggestion.
- Each suggestion supplies the field key, label, and supported custom-field type.
- The executor builds the worksheet and its `Suggested Fields` section from the validated suggestions.
- Keep the result valid JSON and include only fields that the form needs beyond core Unity fields.

## Notes
- The AI output should stay valid JSON.
