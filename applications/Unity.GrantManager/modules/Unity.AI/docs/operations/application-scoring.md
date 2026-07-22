# Application Scoring

## Goal
Generate scored answers for a submitted application against an assigned scoresheet.

## Inputs
- Application submission data
- Assigned scoresheet
- Scoresheet questions and definitions

## Surface
- `POST /api/app/ai/generation/application-scoring`
- `GET /api/app/ai/generation/status`

## Contract
- Structured scoring output. Returns an immediate queued result via API app service, queue, background job, and AI runtime.

## Notes
- The prompt asks for answers only for the configured section or scoresheet context.
- The parsed output must align with the scoresheet question ids.
