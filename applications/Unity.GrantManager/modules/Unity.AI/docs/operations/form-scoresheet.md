# Form Scoresheet

## Goal
Generate a recommended scoresheet definition for a form version.

## Inputs
- Form version context
- Form name
- Existing scoresheet context
- Assigned form and scoresheet identifiers

## Rule
- Generate the assessor rubric for scoring submitted applications.
- Keep the result focused on reviewer criteria, scoring sections, and comments.

## Surface
- `POST /api/app/ai/generation/form-scoresheet`
- `GET /api/app/ai/generation/status`

## Contract
- Structured Flex scoresheet JSON output. Returns an immediate queued result via API app service, queue, background job, and AI runtime.

## Output Shape
- Full scoresheet definition JSON.
- Keep the result focused on assessor criteria, scoring sections, comments, and totals.
- Keep the result valid JSON and compatible with Flex import.

## Notes
- The AI output should stay valid JSON.
