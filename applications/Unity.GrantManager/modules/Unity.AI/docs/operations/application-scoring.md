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
- Structured scoring output. The POST request enqueues generation and returns without the generated payload; clients use the shared status endpoint while the background executor persists the result.

## Notes
- The prompt asks for answers only for the configured section or scoresheet context.
- The parsed output must align with the scoresheet question ids.
