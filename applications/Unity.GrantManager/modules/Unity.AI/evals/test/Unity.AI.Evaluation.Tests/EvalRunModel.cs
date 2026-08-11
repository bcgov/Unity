using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Unity.AI.Evaluation;

// Everything the report writer is allowed to see. Anything sensitive
// (fixture text, rendered prompts, endpoints, keys, RawResponse) must not
// appear on these types. Redaction is by construction: the writer literally
// cannot serialize what isn't here.

public sealed record CaseResult(
    string CaseId,
    string FileName,
    IReadOnlyList<string> Tags,
    bool DeterministicPassed,
    IReadOnlyList<string> DeterministicFailures,
    bool ModelOutputJsonValid,
    JudgeVerdict Judge,
    bool CasePassed,
    string Outcome,
    string FailureCategory,
    string Model,
    string ProviderName,
    string ProfileName,
    string EffectivePromptVersion,
    int? HttpStatusCode,
    string? FinishReason,
    int AttemptCount,
    long DurationMs,
    int PromptTokensTotal,
    int CompletionTokensTotal,
    int TotalTokensTotal,
    int ReasoningTokensTotal,
    bool ExtractionStoppedOnEmpty,
    int ExtractedTextLength,
    string ExtractedTextSha256)
{
    // Candidate text can contain private attachment details. It is excluded from
    // normal reports and is available only to the opt-in private audit writer.
    [JsonIgnore]
    public string CandidateSummary { get; init; } = "";
    public string PromptTemplateSha256 { get; init; } = "";

    public string EvaluationOutcome
    {
        get
        {
            if (HasEvaluationError)
            {
                return "EvaluationError";
            }
            return CasePassed ? "QualityPass" : "QualityFail";
        }
    }

    public bool HasEvaluationError =>
        Judge.Failed
        || !string.Equals(Outcome, "Success", System.StringComparison.Ordinal);

    public bool QualityEligible =>
        !ExtractionStoppedOnEmpty
        && !HasEvaluationError;
}

public sealed record EvalRun(
    string HarnessVersion,
    string CommitSha,
    string DatasetHash,
    string UtcTimestamp,
    string JudgeDeployment,
    string CandidateProvider,
    string CandidateProfile,
    IReadOnlyList<CaseResult> Cases)
{
    public string CaseSetHash { get; init; } = "";
    public string JudgeApiVersion { get; init; } = "";
    public string SourceFilter { get; init; } = "";
    public int AvailableCaseCount { get; init; }
    public bool FullCaseSet { get; init; }
    public IReadOnlyList<string> CandidateModels { get; init; } = System.Array.Empty<string>();
    public IReadOnlyList<string> JudgeModels { get; init; } = System.Array.Empty<string>();
    public IReadOnlyList<string> PromptTemplateHashes { get; init; } = System.Array.Empty<string>();

    public int Total => Cases.Count;
    public int Passed
    {
        get
        {
            var n = 0;
            foreach (var c in Cases)
            {
                if (c.CasePassed) n++;
            }
            return n;
        }
    }
    public double PassRate => Total == 0 ? 0 : (double)Passed / Total;
    public int QualityEligibleCount => Cases.Count(c => c.QualityEligible);
    public int QualityPassed => Cases.Count(c => c.QualityEligible && c.CasePassed);
    public int QualityFailed => Cases.Count(c => c.QualityEligible && !c.CasePassed);
    public double QualityPassRate => QualityEligibleCount == 0
        ? 0
        : (double)QualityPassed / QualityEligibleCount;
    public int EvaluationErrorCount => Cases.Count(c => c.HasEvaluationError);
    public int ExtractionFailureCount => Cases.Count(c => c.ExtractionStoppedOnEmpty);
    public int JudgeCompletedCount => Cases.Count(c => c.Judge.Evaluated);
    public double MeanRubric
    {
        get
        {
            var sum = 0.0;
            var count = 0;
            foreach (var c in Cases)
            {
                if (c.Judge.Evaluated)
                {
                    sum += c.Judge.MeanRubric;
                    count++;
                }
            }
            return count == 0 ? 0 : sum / count;
        }
    }

    public double FactCoverageRate
    {
        get
        {
            var judged = Cases.Where(c => c.Judge.Evaluated).ToList();
            var denominator = judged.Sum(c =>
                c.Judge.CoveredFactCount + c.Judge.PartialFactCount + c.Judge.MissingFactCount);
            if (denominator == 0)
            {
                return 0;
            }

            var numerator = judged.Sum(c =>
                c.Judge.CoveredFactCount + (c.Judge.PartialFactCount * 0.5));
            return numerator / denominator;
        }
    }

    // Candidate = attachment-summary call. Judge = grading call. Both are real
    // per-case AI calls, so both are tracked separately here rather than only
    // reporting a single combined number.
    public long TotalCandidateDurationMs => Cases.Sum(c => c.DurationMs);
    public long TotalJudgeDurationMs => Cases.Sum(c => c.Judge.JudgeDurationMs);
    public double MeanCandidateDurationMs => Total == 0 ? 0 : (double)TotalCandidateDurationMs / Total;
    public double MeanJudgeDurationMs => Total == 0 ? 0 : (double)TotalJudgeDurationMs / Total;

    public int TotalCandidateTokens => Cases.Sum(c => c.TotalTokensTotal);
    public int TotalJudgeTokens => Cases.Sum(c => c.Judge.JudgeTotalTokens);
    public int TotalTokens => TotalCandidateTokens + TotalJudgeTokens;
}
