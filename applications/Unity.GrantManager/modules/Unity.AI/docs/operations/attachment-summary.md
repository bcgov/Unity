# Attachment Summary

## Goal
Generate summaries for selected application attachments.

## Inputs
- One or more attachment IDs
- Application context

## Surface
- `POST /api/app/ai/generation/attachment-summary`
- `GET /api/app/ai/generation/status`

## Contract
- Structured attachment summary output. The POST request enqueues generation and returns without the generated payload; clients use the shared status endpoint while the background executor persists the result.

## Notes
- Each attachment is processed as part of the generation request.
