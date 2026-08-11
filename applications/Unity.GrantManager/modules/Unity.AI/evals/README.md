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

For local runs, `scripts/Invoke-LiveEval.ps1` sets all of the above from a local, gitignored `scripts/live-eval.local.ps1` (copy `live-eval.local.ps1.example` and fill in your endpoint/key once) so a repeat run is one command, e.g. `modules/Unity.AI/evals/scripts/Invoke-LiveEval.ps1 -CaseLimit 3`. The judge deployment name and its `api-version` are independent of the candidate model version -- `api-version` versions the Azure OpenAI REST surface, not the model.

The manual GitHub workflow keeps offline validation separate from the protected live job. The real-case CSV is private and intentionally ignored by Git, so a clean checkout validates only committed synthetic fixtures. The protected live job requires `EVAL_ATTACHMENTS_ARCHIVE_URL`, a secret URL for a ZIP containing both `attachment-summary-eval.csv` and the `<attachment_id>_<file_name>` binaries; files may be nested because the workflow flattens attachments before execution.

Protected live runs set `EVAL_CASE_SOURCE=csv` and are report-only: the suite always runs the complete provisioned real-case set and writes a report (no release gate or baseline-regression check blocks the run). Per-case pass/fail, groundedness, fact coverage, and unsupported/forbidden-claim flags are captured in the report for manual review. Attachments with no extractable text fail the end-to-end case but are excluded from the model-quality denominator because neither the candidate nor judge is invoked. Reports distinguish `QualityPass`, `QualityFail`, and `EvaluationError` and show extraction failures separately. Set `emitBaselineCandidate` to additionally emit `baseline.candidate.json` for manual review — it is not compared against automatically.

For local failure investigation, set `EVAL_PRIVATE_AUDIT_DIR` to a directory outside the normal reports tree. The harness will write candidate summaries plus structured judge fact, claim, and trap assessments there. These private audit files may repeat attachment data and must never be committed or uploaded; normal reports continue to contain only IDs, hashes, aggregate evidence, and operational metrics.
