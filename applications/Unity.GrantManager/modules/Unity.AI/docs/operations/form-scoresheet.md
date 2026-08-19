# Form Scoresheet

## Goal

Generate and publish a scoresheet definition for a form version.

## Inputs

- Form version and form context
- Existing linked scoresheet, when present
- Existing scoresheet sections and fields

## Surface

- `POST /api/app/ai/generation/form-scoresheet`
- `GET /api/app/ai/generation/status`

## Result

The executor validates the generated scoresheet JSON, creates or replaces the form's
scoresheet, publishes it, and links it to the application form.
