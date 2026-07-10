# AI Operation Implementation Playbook

## Purpose
Use this when adding a new AI operation. Start with the bare minimum and only add optional pieces when the operation needs them.

Use these existing operations as the canonical references:

1. `ApplicationAnalysis`
2. `ApplicationScoring`
3. `AttachmentSummary`
4. `FormMapping`
5. `FormScoresheet`
6. `FormWorksheet`

## Base Pattern
1. Define the prompt type.
2. Add the v2 prompt seed.
3. Add the operation seed.
4. Add the runtime contract method.
5. Add the runtime implementation.
6. Add the app service or queue entry.
7. Add the background job only if the result must be applied or persisted.
8. Add the UI button and status polling only if users trigger the operation from the web app.
9. Add tests for the prompt, runtime parsing, and job or service path.

## Bare Minimum
For the first pass, only add what is required for a working operation:

- prompt type
- prompt seed
- operation seed
- runtime method
- queue/app service entry
- job or direct apply path, if needed

## Optional Pieces
Add these only when the operation needs them:

- feature flag
- permissions
- permission definition provider entries
- menu entry
- UI button
- status polling
- refresh-after-complete behavior
- persistence/import/publish/assign behavior

## Rules
- Keep the prompt as the source of truth.
- Reuse the existing async generation pattern.
- Do not hardcode field buckets or response shapes in UI code.
- Do not invent new plumbing if an existing operation already does the same job.
- Do not add tenant feature seeding.
- Do not add write-back UI behavior unless the operation already persists output.

## Expected Flow
1. User clicks Generate.
2. UI disables the button and shows generating state, if the operation has UI.
3. API checks permission and feature flag, if the operation uses them.
4. API queues the generation request.
5. Background job loads the operation context.
6. Job builds the prompt payload from existing data.
7. AI runtime renders v2 prompts and logs input/output.
8. Job parses the AI response.
9. Job applies the result if needed.
10. Job stamps status and rate limit state.
11. UI polls status and refreshes after completion, if applicable.

## Validation
- Confirm the prompt version is v2.
- Confirm the operation exists in the AI operation seed.
- Confirm any required feature flag exists in the host feature definitions.
- Confirm any required permission is wired in the permission definition provider.
- Confirm the UI button uses the same generating/status flow as the other operations, if it is user-triggered.
