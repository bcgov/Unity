# AI Operation Implementation Playbook

## Purpose
Use this when adding a new AI operation. Start with the bare minimum and only add optional pieces when the operation needs them.

Use these existing operations as the canonical references:

1. `ApplicationAnalysis`
2. `ApplicationScoring`
3. `AttachmentSummary`
4. `FormMapping`
5. `FormWorksheet`
6. `FormScoresheet`

## Base Pattern
1. Add the catalog definition, prompt family, model/operation seed, feature, and permissions.
2. Add supported prompt versions; do not assume a specific version number.
3. Add the runtime request/response contract and implementation.
4. Add an executor when Grant Manager must load input or persist a result.
5. Register the executor through the existing transient DI convention.
6. Expose a generate surface and UI only when users need one.
7. Add focused catalog, runtime, executor, and persistence tests.

## Staged form mapping

`FormMapping` may be part of the staged form-configuration flow:

1. Generate mapping suggestions without overwriting the saved mapping.
2. Persist suggestions for review; accept them individually so existing
   non-empty mappings always win.
3. Optionally generate and review `FormWorksheet` suggestions.
4. Run final mapping with accepted worksheet fields in its context.

Keep mapping review state scoped to the form version. Do not auto-link unpublished
AI worksheet drafts to a UI anchor just to make their fields visible to mapping.

## Rules
- Keep prompt content and operation/model configuration in the database.
- Reuse the shared generation pipeline.
- Do not hardcode field buckets or response shapes in UI code.
- Do not invent new plumbing if an existing operation already does the same job.
- Keep operation-specific input and persistence in the executor.
- Do not add UI write-back behavior unless the operation persists its output.

## Validation
- Confirm the operation exists in the catalog and host seed.
- Confirm every supported prompt version resolves correctly.
- Confirm any required feature flag exists in the host feature definitions.
- Confirm any required permission is wired in the permission definition provider.
- Confirm the UI button uses the same generating/status flow as the other operations, if it is user-triggered.
