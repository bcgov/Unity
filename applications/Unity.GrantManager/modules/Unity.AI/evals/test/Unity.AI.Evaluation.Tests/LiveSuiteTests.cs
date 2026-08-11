using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Unity.AI.Extraction;
using Unity.AI.Operations;
using Unity.AI.Requests;
using Unity.AI.Runtime.Execution;
using Unity.GrantManager;
using Xunit;
using Xunit.Abstractions;

namespace Unity.AI.Evaluation;

// Live suite: hits the real Azure OpenAI candidate + judge. Requires both
// candidate config (via ABP `IConfiguration`) and judge env vars. If any
// judge env var is missing, both facts no-op with a printed message — the
// suite is designed to be filtered in CI (`--filter Category=AIEvalLive`)
// only when the protected environment provides the secrets.
[Trait("Category", "AIEvalLive")]
[Collection("AIEvalLive")]
public class LiveSuiteTests : GrantManagerApplicationTestBase
{
    private const string HarnessVersion = "0.4.0";
    private readonly ITestOutputHelper _out;

    public LiveSuiteTests(ITestOutputHelper output) : base(output)
    {
        _out = output;
    }

    [Fact]
    public async Task Run_Live_Suite()
    {
        if (!LiveRunEnabled())
        {
            _out.WriteLine("SKIP: EVAL_RUN_LIVE!=1.");
            return;
        }
        EnsureLiveEnvironment();

        var run = await ExecuteSuiteAsync(CancellationToken.None);
        var outDir = ReportWriter.Write(run, DatasetLoader.ReportsRoot);
        _out.WriteLine($"report: {outDir}");
        var privateAuditPath = PrivateAuditWriter.WriteIfEnabled(run);
        if (privateAuditPath != null)
        {
            _out.WriteLine($"private audit: {privateAuditPath}");
        }
    }

    [Fact]
    public async Task Emit_Baseline_Candidate()
    {
        if (!LiveRunEnabled())
        {
            _out.WriteLine("SKIP: EVAL_RUN_LIVE!=1.");
            return;
        }

        var emit = Environment.GetEnvironmentVariable("EVAL_EMIT_BASELINE");
        if (!string.Equals(emit, "1", StringComparison.Ordinal))
        {
            _out.WriteLine("SKIP: EVAL_EMIT_BASELINE!=1.");
            return;
        }
        EnsureLiveEnvironment();

        var run = await ExecuteSuiteAsync(CancellationToken.None);
        var baseline = BuildBaseline(run);

        var candidatePath = Path.Combine(DatasetLoader.WritableDatasetRoot, "baseline.candidate.json");
        Directory.CreateDirectory(DatasetLoader.WritableDatasetRoot);
        var json = JsonSerializer.Serialize(baseline, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(candidatePath, json);
        _out.WriteLine($"wrote candidate baseline: {candidatePath}");
    }

    private async Task<EvalRun> ExecuteSuiteAsync(CancellationToken ct)
    {
        var cases = DatasetLoader.LoadCases(requireCsvAttachments: true);
        cases.ShouldNotBeEmpty();

        // EVAL_CASE_SOURCE restricts to one origin ("csv" = real downloaded
        // attachments, "jsonl" = committed synthetic fixtures) before any limit
        // is applied, so a smoke run can target real cases without also pulling
        // in the synthetic ones that happen to load first.
        var sourceFilter = Environment.GetEnvironmentVariable("EVAL_CASE_SOURCE");
        if (!string.IsNullOrWhiteSpace(sourceFilter))
        {
            cases = cases
                .Where(c => string.Equals(c.Source, sourceFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();
            cases.ShouldNotBeEmpty();
            _out.WriteLine($"EVAL_CASE_SOURCE={sourceFilter}: restricted to {cases.Count} case(s).");
        }

        var availableCases = cases.ToList();
        var availableCaseCount = availableCases.Count;

        var explicitCaseIdsRaw = Environment.GetEnvironmentVariable("EVAL_CASE_IDS");
        if (!string.IsNullOrWhiteSpace(explicitCaseIdsRaw))
        {
            var requestedIds = explicitCaseIdsRaw
                .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var availableIds = cases.Select(c => c.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missingIds = requestedIds.Where(id => !availableIds.Contains(id)).ToList();
            missingIds.ShouldBeEmpty(
                $"EVAL_CASE_IDS contains unknown case(s): {string.Join(", ", missingIds)}");
            var requestedSet = requestedIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
            cases = cases.Where(c => requestedSet.Contains(c.Id)).ToList();
            cases.ShouldNotBeEmpty();
            _out.WriteLine($"EVAL_CASE_IDS: selected {cases.Count} explicit case(s).");
        }

        // EVAL_CASE_OFFSET skips the first N cases (after source filtering) so
        // repeated smoke runs can walk through distinct cases instead of always
        // re-running case 0.
        var caseOffsetRaw = Environment.GetEnvironmentVariable("EVAL_CASE_OFFSET");
        if (int.TryParse(caseOffsetRaw, out var caseOffset) && caseOffset > 0)
        {
            cases = cases.Skip(caseOffset).ToList();
            cases.ShouldNotBeEmpty();
            _out.WriteLine($"EVAL_CASE_OFFSET={caseOffset}: {cases.Count} case(s) remain.");
        }

        var caseLimitRaw = Environment.GetEnvironmentVariable("EVAL_CASE_LIMIT");
        if (int.TryParse(caseLimitRaw, out var caseLimit) && caseLimit > 0 && caseLimit < cases.Count)
        {
            cases = cases.Take(caseLimit).ToList();
            _out.WriteLine($"EVAL_CASE_LIMIT={caseLimit}: running a subset of {cases.Count} case(s).");
        }

        var csvCount = cases.Count(c => c.AttachmentAbsolutePath != null);
        _out.WriteLine($"cases: {cases.Count} (csv-derived with real attachments: {csvCount}, attachments root: {DatasetLoader.AttachmentsRoot})");

        var evalService = GetRequiredService<IAttachmentSummaryEvaluationService>();
        var extractor = GetRequiredService<ITextExtractionService>();
        using var judge = new AzureOpenAIJudgeClient();

        var results = new List<CaseResult>();
        string candidateProvider = "";
        string candidateProfile = "";

        foreach (var c in cases)
        {
            var extraction = await EvalAttachmentReader.ExtractAsync(c, extractor, ct);
            var extracted = extraction.ExtractedText;
            AttachmentSummaryDiagnosticResult diag;
            if (extraction.StoppedOnEmpty)
            {
                diag = EmptyExtractionDiagnostic();
            }
            else
            {
                var request = new AttachmentSummaryRequest
                {
                    FileName = c.FileName,
                    ContentType = string.IsNullOrWhiteSpace(c.ContentType) ? "application/octet-stream" : c.ContentType,
                    ExtractedText = extracted,
                    PromptVersion = c.PromptVersion,
                };

                diag = await evalService.RunAsync(request, ct);
                candidateProvider = diag.ProviderName;
                candidateProfile = diag.ProfileName;
            }

            var det = DeterministicChecks.Run(c, diag.Summary, diag.ModelOutput, diag.Outcome);
            var verdict = extraction.StoppedOnEmpty
                ? JudgeVerdict.Skip("no extractable text")
                : await judge.JudgeAsync(
                    c,
                    extracted,
                    diag.Summary,
                    extraction.StoppedOnEmpty,
                    ct);
            var casePassed = EvalCasePassPolicy.Passes(
                det,
                verdict,
                extraction.StoppedOnEmpty);

            results.Add(new CaseResult(
                CaseId: c.Id,
                FileName: c.FileName,
                Tags: c.Tags,
                DeterministicPassed: det.Passed,
                DeterministicFailures: det.Failures,
                ModelOutputJsonValid: det.ModelOutputJsonValid,
                Judge: verdict,
                CasePassed: casePassed,
                Outcome: diag.Outcome.ToString(),
                FailureCategory: diag.FailureCategory.ToString(),
                Model: diag.Model ?? "",
                ProviderName: diag.ProviderName,
                ProfileName: diag.ProfileName,
                EffectivePromptVersion: diag.EffectivePromptVersion,
                HttpStatusCode: diag.HttpStatusCode,
                FinishReason: diag.FinishReason,
                AttemptCount: diag.AttemptCount,
                DurationMs: diag.DurationMs,
                PromptTokensTotal: diag.PromptTokensTotal,
                CompletionTokensTotal: diag.CompletionTokensTotal,
                TotalTokensTotal: diag.TotalTokensTotal,
                ReasoningTokensTotal: diag.ReasoningTokensTotal,
                ExtractionStoppedOnEmpty: extraction.StoppedOnEmpty,
                ExtractedTextLength: extracted?.Length ?? 0,
                ExtractedTextSha256: Sha256Hex(extracted ?? ""))
            {
                CandidateSummary = diag.Summary,
                PromptTemplateSha256 = diag.PromptTemplateSha256,
            });
        }

        var selectedCaseSetHash = DatasetHasher.ComputeCaseSet(cases);
        var availableCaseSetHash = DatasetHasher.ComputeCaseSet(availableCases);
        return new EvalRun(
            HarnessVersion: HarnessVersion,
            CommitSha: Environment.GetEnvironmentVariable("GITHUB_SHA") ?? "local",
            DatasetHash: DatasetHasher.Compute(),
            UtcTimestamp: DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ"),
            JudgeDeployment: judge.DeploymentName,
            CandidateProvider: candidateProvider,
            CandidateProfile: candidateProfile,
            Cases: results)
        {
            CaseSetHash = selectedCaseSetHash,
            JudgeApiVersion = judge.ApiVersion,
            SourceFilter = sourceFilter ?? "",
            AvailableCaseCount = availableCaseCount,
            FullCaseSet = string.Equals(
                selectedCaseSetHash,
                availableCaseSetHash,
                StringComparison.Ordinal),
            CandidateModels = results
                .Select(result => result.Model)
                .Where(model => !string.IsNullOrWhiteSpace(model))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(model => model, StringComparer.Ordinal)
                .ToList(),
            JudgeModels = results
                .Select(result => result.Judge.JudgeModel)
                .Where(model => !string.IsNullOrWhiteSpace(model))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(model => model, StringComparer.Ordinal)
                .ToList(),
            PromptTemplateHashes = results
                .Select(result => result.PromptTemplateSha256)
                .Where(hash => !string.IsNullOrWhiteSpace(hash)
                    && !string.Equals(hash, "not-invoked", StringComparison.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(hash => hash, StringComparer.Ordinal)
                .ToList(),
        };
    }

    private static AttachmentSummaryDiagnosticResult EmptyExtractionDiagnostic()
    {
        return new AttachmentSummaryDiagnosticResult(
            Summary: AttachmentSummaryExtractor.TextExtractionFailedSummary,
            ModelOutput: string.Empty,
            Outcome: AIOperationOutcome.Success,
            FailureCategory: AIFailureCategory.None,
            EffectivePromptVersion: "not-invoked",
            ProviderName: "pipeline",
            ProfileName: "extraction-short-circuit",
            Model: null,
            HttpStatusCode: null,
            FinishReason: null,
            AttemptCount: 0,
            DurationMs: 0,
            PromptTokensTotal: 0,
            CompletionTokensTotal: 0,
            TotalTokensTotal: 0,
            ReasoningTokensTotal: 0)
        {
            PromptTemplateSha256 = "not-invoked",
        };
    }

    private static Baseline BuildBaseline(EvalRun run)
    {
        var cases = new Dictionary<string, BaselineCase>();
        var firstPromptVersion = run.Cases
            .Select(result => result.EffectivePromptVersion)
            .FirstOrDefault(version => !string.Equals(
                version,
                "not-invoked",
                StringComparison.Ordinal))
            ?? "";
        foreach (var c in run.Cases)
        {
            cases[c.CaseId] = new BaselineCase
            {
                Pass = c.CasePassed,
                Hallucination = c.Judge.Hallucination,
                ForbiddenClaim = c.Judge.ForbiddenClaim,
                FactCoverageRate = c.Judge.FactCoverageRate,
                Rubric = new BaselineRubric
                {
                    Groundedness = c.Judge.Groundedness,
                    RequiredFactCoverage = c.Judge.RequiredFactCoverage,
                    AttachmentFocus = c.Judge.AttachmentFocus,
                    ReviewerUsefulness = c.Judge.ReviewerUsefulness,
                },
            };
        }
        return new Baseline
        {
            BaselineVersion = 2,
            DatasetHash = run.DatasetHash,
            HarnessVersion = run.HarnessVersion,
            CaseSetHash = run.CaseSetHash,
            Candidate = new BaselineCandidate
            {
                Provider = run.CandidateProvider,
                Profile = run.CandidateProfile,
                PromptVersion = firstPromptVersion,
                Models = run.CandidateModels.ToList(),
                PromptTemplateHashes = run.PromptTemplateHashes.ToList(),
            },
            Judge = new BaselineJudge
            {
                Deployment = run.JudgeDeployment,
                ApiVersion = run.JudgeApiVersion,
                Models = run.JudgeModels.ToList(),
            },
            Aggregate = new BaselineAggregate
            {
                PassRate = run.PassRate,
                MeanRubric = run.MeanRubric,
                FactCoverageRate = run.FactCoverageRate,
            },
            Cases = cases,
        };
    }

    private static bool LiveRunEnabled()
    {
        return string.Equals(
            Environment.GetEnvironmentVariable("EVAL_RUN_LIVE"),
            "1",
            StringComparison.Ordinal);
    }

    private static void EnsureLiveEnvironment()
    {
        var provider = Environment.GetEnvironmentVariable("Azure__Operations__Defaults__Provider");
        var required = new List<string>
        {
            "Azure__Operations__Defaults__Provider",
            "EVAL_JUDGE_ENDPOINT",
            "EVAL_JUDGE_KEY",
            "EVAL_JUDGE_DEPLOYMENT",
            "EVAL_JUDGE_API_VERSION",
        };

        if (!string.IsNullOrWhiteSpace(provider))
        {
            required.Add($"Azure__{provider}__Endpoint");
            required.Add($"Azure__{provider}__ApiKey");
        }

        var missing = required
            .Where(name => string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name)))
            .ToList();
        if (missing.Count > 0)
        {
            throw new InvalidOperationException(
                $"Missing live evaluation environment variable(s): {string.Join(", ", missing)}.");
        }
    }

    private static string Sha256Hex(string s)
    {
        var bytes = Encoding.UTF8.GetBytes(s);
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }
}
