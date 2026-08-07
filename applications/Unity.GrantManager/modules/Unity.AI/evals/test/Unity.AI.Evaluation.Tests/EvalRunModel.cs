using System.Collections.Generic;

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
    string ExtractedTextSha256);

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
    public double MeanRubric
    {
        get
        {
            var sum = 0.0;
            var count = 0;
            foreach (var c in Cases)
            {
                if (!c.Judge.Failed)
                {
                    sum += c.Judge.MeanRubric;
                    count++;
                }
            }
            return count == 0 ? 0 : sum / count;
        }
    }
}
