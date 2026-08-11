# Runs the protected live attachment-summary suite (Category=AIEvalLive)
# against the real Azure OpenAI candidate + judge, using credentials from a
# local, gitignored live-eval.local.ps1 (see live-eval.local.ps1.example).
#
# Usage (from anywhere):
#   modules/Unity.AI/evals/scripts/Invoke-LiveEval.ps1 -CaseLimit 3
#   modules/Unity.AI/evals/scripts/Invoke-LiveEval.ps1 -CaseLimit 3 -CaseOffset 3
#   modules/Unity.AI/evals/scripts/Invoke-LiveEval.ps1 -EmitBaseline

param(
    [string]$CaseSource = "csv",
    [string[]]$CaseIds = @(),
    [int]$CaseLimit = 0,
    [int]$CaseOffset = 0,
    [string]$PrivateAuditDir = "",
    [switch]$EmitBaseline
)

$ErrorActionPreference = 'Stop'

$localSecrets = Join-Path $PSScriptRoot 'live-eval.local.ps1'
if (-not (Test-Path $localSecrets)) {
    throw "Missing $localSecrets. Copy live-eval.local.ps1.example to live-eval.local.ps1 (same folder) and fill in your Azure OpenAI endpoint/key. It is gitignored -- never commit it."
}
. $localSecrets

$required = @(
    'Azure__Operations__Defaults__Provider',
    'Azure__OpenAI__Endpoint',
    'Azure__OpenAI__ApiKey',
    'Azure__OpenAI__Profiles__Gpt5Mini__DeploymentName',
    'EVAL_JUDGE_ENDPOINT',
    'EVAL_JUDGE_KEY',
    'EVAL_JUDGE_DEPLOYMENT',
    'EVAL_JUDGE_API_VERSION'
)
$missing = $required | Where-Object { [string]::IsNullOrWhiteSpace([Environment]::GetEnvironmentVariable($_)) }
if ($missing) {
    throw "live-eval.local.ps1 is missing value(s) for: $($missing -join ', ')"
}

$env:EVAL_RUN_LIVE = "1"
$env:EVAL_CASE_SOURCE = $CaseSource
if ($CaseIds.Count -gt 0) { $env:EVAL_CASE_IDS = $CaseIds -join ',' } else { Remove-Item Env:EVAL_CASE_IDS -ErrorAction SilentlyContinue }
if ($CaseLimit -gt 0) { $env:EVAL_CASE_LIMIT = "$CaseLimit" } else { Remove-Item Env:EVAL_CASE_LIMIT -ErrorAction SilentlyContinue }
if ($CaseOffset -gt 0) { $env:EVAL_CASE_OFFSET = "$CaseOffset" } else { Remove-Item Env:EVAL_CASE_OFFSET -ErrorAction SilentlyContinue }
$env:EVAL_EMIT_BASELINE = if ($EmitBaseline) { "1" } else { "0" }
if ([string]::IsNullOrWhiteSpace($PrivateAuditDir)) {
    Remove-Item Env:EVAL_PRIVATE_AUDIT_DIR -ErrorAction SilentlyContinue
} else {
    $env:EVAL_PRIVATE_AUDIT_DIR = $PrivateAuditDir
}

$testName = if ($EmitBaseline) { 'Emit_Baseline_Candidate' } else { 'Run_Live_Suite' }

# modules/Unity.AI/evals/scripts -> applications/Unity.GrantManager
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..\..\..')

Push-Location $repoRoot
try {
    dotnet test modules/Unity.AI/evals/test/Unity.AI.Evaluation.Tests/Unity.AI.Evaluation.Tests.csproj `
        --filter "Category=AIEvalLive&FullyQualifiedName~$testName" `
        --logger "console;verbosity=detailed"
}
finally {
    Pop-Location
}
