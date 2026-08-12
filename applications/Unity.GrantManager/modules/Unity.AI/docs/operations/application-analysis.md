# Application Analysis

## Goal
Generate an AI analysis of an application submission.

## Inputs
- Application submission data
- Application context
- Optional attachments, when present

## Surface
- `POST /api/app/ai/generation/application-analysis`
- `GET /api/app/ai/generation/status`

## Contract
- Structured analysis output. The POST request enqueues generation and returns without the generated payload; clients use the shared status endpoint while the background executor persists the result.

## Notes
- This is a reviewer-oriented summary and recommendation flow.
