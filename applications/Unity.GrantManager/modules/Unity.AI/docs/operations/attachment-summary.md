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
- Structured attachment summary output. Returns an immediate queued result via API app service, queue, background job, and AI runtime.

## Notes
- Each attachment is processed as part of the generation request.
