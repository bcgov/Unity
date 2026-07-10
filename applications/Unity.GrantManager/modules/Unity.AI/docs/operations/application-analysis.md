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
- Structured analysis output. Returns an immediate queued result via API app service, queue, background job, and AI runtime.

## Notes
- This is a reviewer-oriented summary and recommendation flow.
