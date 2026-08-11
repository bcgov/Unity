using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Unity.AI.Evaluation;

// Redaction by construction: only fields on EvalRun/CaseResult are written.
// This class never sees fixture text, rendered prompts, endpoints, keys,
// or RawResponse — those types don't exist on its inputs.
internal static class ReportWriter
{
    public static string Write(EvalRun run, string reportsRoot)
    {
        var outDir = Path.Combine(reportsRoot, run.UtcTimestamp);
        Directory.CreateDirectory(outDir);

        var json = JsonSerializer.Serialize(run, new JsonSerializerOptions
        {
            WriteIndented = true,
        });
        File.WriteAllText(Path.Combine(outDir, "run.json"), json);

        File.WriteAllText(
            Path.Combine(outDir, "results.csv"),
            BuildCsv(run),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        File.WriteAllText(Path.Combine(outDir, "summary.md"), BuildSummary(run));

        return outDir;
    }

    private static string BuildCsv(EvalRun run)
    {
        var sb = new StringBuilder();
        sb.AppendLine("caseId,fileName,passed,evaluationOutcome,candidateOutcome,failureCategory,detPassed,jsonOutputValid,judgeFailure,judgeSkipped,groundedness,requiredFactCoverage,factCoverageRate,coveredFacts,partialFacts,missingFacts,missingFactIds,attachmentFocus,reviewerUsefulness,hallucination,unsupportedClaims,hallucinationSeverity,forbiddenClaim,triggeredTrapIds,extractionStoppedOnEmpty,httpStatus,finishReason,attempts,durationMs,promptTokens,completionTokens,totalTokens,reasoningTokens,extractedLen,extractedSha256,promptTemplateSha256,judgeDurationMs,judgeAttempts,judgePromptTokens,judgeCompletionTokens,judgeTotalTokens,judgeReasoningTokens");
        foreach (var c in run.Cases)
        {
            sb.Append(Csv(c.CaseId)).Append(',');
            sb.Append(Csv(c.FileName)).Append(',');
            sb.Append(c.CasePassed).Append(',');
            sb.Append(Csv(c.EvaluationOutcome)).Append(',');
            sb.Append(Csv(c.Outcome)).Append(',');
            sb.Append(Csv(c.FailureCategory)).Append(',');
            sb.Append(c.DeterministicPassed).Append(',');
            sb.Append(c.ModelOutputJsonValid).Append(',');
            sb.Append(Csv(c.Judge.FailureReason ?? "")).Append(',');
            sb.Append(Csv(c.Judge.SkippedReason ?? "")).Append(',');
            sb.Append(c.Judge.Groundedness).Append(',');
            sb.Append(c.Judge.RequiredFactCoverage).Append(',');
            sb.Append(c.Judge.FactCoverageRate.ToString("F3", CultureInfo.InvariantCulture)).Append(',');
            sb.Append(c.Judge.CoveredFactCount).Append(',');
            sb.Append(c.Judge.PartialFactCount).Append(',');
            sb.Append(c.Judge.MissingFactCount).Append(',');
            sb.Append(Csv(string.Join("|", c.Judge.MissingFactIds))).Append(',');
            sb.Append(c.Judge.AttachmentFocus).Append(',');
            sb.Append(c.Judge.ReviewerUsefulness).Append(',');
            sb.Append(c.Judge.Hallucination).Append(',');
            sb.Append(c.Judge.UnsupportedClaimCount).Append(',');
            sb.Append(Csv(c.Judge.HallucinationSeverity)).Append(',');
            sb.Append(c.Judge.ForbiddenClaim).Append(',');
            sb.Append(Csv(string.Join("|", c.Judge.TriggeredTrapIds))).Append(',');
            sb.Append(c.ExtractionStoppedOnEmpty).Append(',');
            sb.Append(c.HttpStatusCode?.ToString(CultureInfo.InvariantCulture) ?? "").Append(',');
            sb.Append(Csv(c.FinishReason ?? "")).Append(',');
            sb.Append(c.AttemptCount).Append(',');
            sb.Append(c.DurationMs).Append(',');
            sb.Append(c.PromptTokensTotal).Append(',');
            sb.Append(c.CompletionTokensTotal).Append(',');
            sb.Append(c.TotalTokensTotal).Append(',');
            sb.Append(c.ReasoningTokensTotal).Append(',');
            sb.Append(c.ExtractedTextLength).Append(',');
            sb.Append(c.ExtractedTextSha256).Append(',');
            sb.Append(c.PromptTemplateSha256).Append(',');
            sb.Append(c.Judge.JudgeDurationMs).Append(',');
            sb.Append(c.Judge.JudgeAttemptCount).Append(',');
            sb.Append(c.Judge.JudgePromptTokens).Append(',');
            sb.Append(c.Judge.JudgeCompletionTokens).Append(',');
            sb.Append(c.Judge.JudgeTotalTokens).Append(',');
            sb.Append(c.Judge.JudgeReasoningTokens).AppendLine();
        }
        return sb.ToString();
    }

    private static string Csv(string v)
    {
        if (v.Contains(',') || v.Contains('"'))
        {
            return "\"" + v.Replace("\"", "\"\"") + "\"";
        }
        return v;
    }

    private static string BuildSummary(EvalRun run)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Attachment Summary Eval Run");
        sb.AppendLine();
        sb.AppendLine($"- Timestamp: `{run.UtcTimestamp}`");
        sb.AppendLine($"- Commit: `{run.CommitSha}`");
        sb.AppendLine($"- Harness: `{run.HarnessVersion}`");
        sb.AppendLine($"- Dataset hash: `{run.DatasetHash}`");
        sb.AppendLine($"- Case-set hash: `{run.CaseSetHash}`");
        sb.AppendLine($"- Source filter: `{run.SourceFilter}`");
        sb.AppendLine($"- Full case set: `{run.FullCaseSet}` ({run.Total}/{run.AvailableCaseCount})");
        var candidateModelLabel = run.CandidateModels.Count > 0
            ? string.Join(", ", run.CandidateModels)
            : "(unknown)";
        sb.AppendLine($"- Candidate: `{run.CandidateProvider}/{candidateModelLabel}` (profile: `{run.CandidateProfile}`)");
        sb.AppendLine($"- Prompt template hash(es): `{string.Join(", ", run.PromptTemplateHashes)}`");
        sb.AppendLine($"- Judge deployment: `{run.JudgeDeployment}`");
        sb.AppendLine($"- Judge model(s): `{string.Join(", ", run.JudgeModels)}`");
        sb.AppendLine($"- Judge API version: `{run.JudgeApiVersion}`");
        sb.AppendLine();
        sb.AppendLine($"- Total: {run.Total}");
        sb.AppendLine($"- End-to-end passed: {run.Passed}/{run.Total}");
        sb.AppendLine($"- End-to-end pass rate: {run.PassRate.ToString("P1", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"- Quality pass: {run.QualityPassed}/{run.QualityEligibleCount} ({run.QualityPassRate.ToString("P1", CultureInfo.InvariantCulture)})");
        sb.AppendLine($"- Quality fail: {run.QualityFailed}");
        sb.AppendLine($"- Evaluation errors: {run.EvaluationErrorCount}");
        sb.AppendLine($"- Extraction failures: {run.ExtractionFailureCount}");
        sb.AppendLine($"- Judge completed: {run.JudgeCompletedCount}/{run.Total}");
        sb.AppendLine($"- Mean rubric score: {run.MeanRubric.ToString("F2", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"- Fact coverage: {run.FactCoverageRate.ToString("P1", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"- Minor unsupported-claim warnings: {run.Cases.Count(c => c.Judge.HasMinorUnsupportedClaimWarning)}");
        sb.AppendLine($"- Blocking unsupported-claim cases: {run.Cases.Count(c => c.Judge.HasBlockingUnsupportedClaim)}");
        sb.AppendLine();
        sb.AppendLine("## Latency & tokens");
        sb.AppendLine();
        sb.AppendLine($"- Candidate: mean {run.MeanCandidateDurationMs.ToString("F0", CultureInfo.InvariantCulture)} ms/case, total {FormatDuration(run.TotalCandidateDurationMs)}, {run.TotalCandidateTokens:N0} tokens");
        sb.AppendLine($"- Judge: mean {run.MeanJudgeDurationMs.ToString("F0", CultureInfo.InvariantCulture)} ms/case, total {FormatDuration(run.TotalJudgeDurationMs)}, {run.TotalJudgeTokens:N0} tokens");
        sb.AppendLine($"- Combined tokens (candidate + judge): {run.TotalTokens:N0}");
        sb.AppendLine();
        sb.AppendLine("| Case | Pass | Eval outcome | Candidate | Det | JSON | Empty | G | RF | FC | Miss | AF | RU | Claims | U | Sev | Forb | Traps | Cand ms | Cand tok | Judge ms | Judge tok |");
        sb.AppendLine("|------|------|--------------|-----------|-----|------|-------|---|----|----|------|----|----|--------|---|-----|------|-------|---------|----------|----------|-----------|");
        foreach (var c in run.Cases)
        {
            sb.Append("| ").Append(c.CaseId)
              .Append(" | ").Append(c.CasePassed ? "OK" : "FAIL")
              .Append(" | ").Append(c.EvaluationOutcome)
              .Append(" | ").Append(c.Outcome)
              .Append(" | ").Append(c.DeterministicPassed ? "OK" : "FAIL")
              .Append(" | ").Append(c.ModelOutputJsonValid ? "Y" : "N")
              .Append(" | ").Append(c.ExtractionStoppedOnEmpty ? "Y" : "N")
              .Append(" | ").Append(c.Judge.Groundedness)
              .Append(" | ").Append(c.Judge.RequiredFactCoverage)
              .Append(" | ").Append(c.Judge.FactCoverageRate.ToString("P0", CultureInfo.InvariantCulture))
              .Append(" | ").Append(string.Join(",", c.Judge.MissingFactIds))
              .Append(" | ").Append(c.Judge.AttachmentFocus)
              .Append(" | ").Append(c.Judge.ReviewerUsefulness)
              .Append(" | ").Append(UnsupportedClaimDisposition(c.Judge))
              .Append(" | ").Append(c.Judge.UnsupportedClaimCount)
              .Append(" | ").Append(c.Judge.HallucinationSeverity)
              .Append(" | ").Append(c.Judge.ForbiddenClaim ? "Y" : "N")
              .Append(" | ").Append(string.Join(",", c.Judge.TriggeredTrapIds))
              .Append(" | ").Append(c.DurationMs)
              .Append(" | ").Append(c.TotalTokensTotal)
              .Append(" | ").Append(c.Judge.JudgeDurationMs)
              .Append(" | ").Append(c.Judge.JudgeTotalTokens)
              .AppendLine(" |");
        }
        return sb.ToString();
    }

    private static string UnsupportedClaimDisposition(JudgeVerdict verdict)
    {
        if (verdict.HasMinorUnsupportedClaimWarning)
        {
            return "WARN";
        }

        return verdict.HasBlockingUnsupportedClaim ? "BLOCK" : "-";
    }

    private static string FormatDuration(long milliseconds)
    {
        return TimeSpan.FromMilliseconds(milliseconds).ToString(@"hh\:mm\:ss");
    }
}
