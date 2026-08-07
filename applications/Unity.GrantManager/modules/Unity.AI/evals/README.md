# Attachment-summary evaluation

This dataset exercises the production attachment text extractor and attachment-summary prompt.

## Dataset layout

- `data/attachment-summary-eval.csv` contains 27 cases with structured metadata, extraction fingerprints, atomic facts, evidence locations, baseline summaries, and hallucination traps.
- `dataset/attachments/` contains private downloaded binaries named `<attachment_id>_<file_name>`.
- `scripts/Get-EvalAttachments.ps1` downloads CSV attachments from the dev environment.
- `test/Unity.AI.Evaluation.Tests` contains the offline validator and protected live model/judge suite.

The attachment directory is ignored by Git. Never commit the downloaded files or extracted text. Reports contain IDs, hashes, scores, and operational metrics, but not document text or model output.

## CSV schema

Source identity is stored in `tenant`, `attachment_id`, `file_name`, `chefs_submission_id`, and `chefs_file_id`. The file extension is derived from `file_name`.

Review metadata is stored in:

- `tags_json`, including document type, document state, difficulty, and trap categories
- `baseline_summary`, the reference summary used by the evaluation
- `expected_facts_json`, a JSON array of 2-5 atomic facts with source evidence locations
- `hallucination_traps_json`, a JSON array of explicit forbidden claims and trap types

Private source text remains runtime-only and is not a CSV column. The loader extracts it directly from the private attachment. `extraction_status`, `extracted_text_length`, and `extracted_text_sha256` record the verified production-extraction result. A changed binary or extraction policy therefore fails offline validation without committing the extracted text.

## Local validation

From `applications/Unity.GrantManager`:

```powershell
dotnet test modules/Unity.AI/evals/test/Unity.AI.Evaluation.Tests/Unity.AI.Evaluation.Tests.csproj `
  --filter "Category=AIEvalOffline"
```

When the private binaries are present, the offline suite also verifies that every CSV row has exactly one file and that the production extraction length and SHA-256 match the reviewed fingerprint. Cases with `extraction_status=no_text_verified` must take the production short-circuit path.

To download or refresh the binaries:

```powershell
modules/Unity.AI/evals/scripts/Get-EvalAttachments.ps1
```

Set `EVAL_ATTACHMENTS_DIR` to use a different attachment directory.

## Live run

The live suite requires `EVAL_RUN_LIVE=1`, the candidate provider settings, and:

- `EVAL_JUDGE_ENDPOINT`
- `EVAL_JUDGE_KEY`
- `EVAL_JUDGE_DEPLOYMENT`
- `EVAL_JUDGE_API_VERSION`

It fails if any CSV attachment is missing. Results are written to `test/Unity.AI.Evaluation.Tests/reports/` (relative to this directory). Set `EVAL_EMIT_BASELINE=1` to create `baseline.candidate.json` in the source dataset directory.

The manual GitHub workflow keeps offline validation separate from the protected live job. The live job requires `EVAL_ATTACHMENTS_ARCHIVE_URL`, a secret URL for a ZIP whose root contains the `<attachment_id>_<file_name>` files.
