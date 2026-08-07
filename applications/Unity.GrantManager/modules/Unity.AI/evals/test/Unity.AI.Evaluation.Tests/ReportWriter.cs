using System;
using System.Globalization;
using System.IO;
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

        File.WriteAllText(Path.Combine(outDir, "results.csv"), BuildCsv(run));
        File.WriteAllText(Path.Combine(outDir, "summary.md"), BuildSummary(run));

        return outDir;
    }

    private static string BuildCsv(EvalRun run)
    {
        var sb = new StringBuilder();
        sb.AppendLine("caseId,fileName,passed,outcome,failureCategory,detPassed,jsonOutputValid,judgeFailure,groundedness,requiredFactCoverage,attachmentFocus,reviewerUsefulness,hallucination,forbiddenClaim,extractionStoppedOnEmpty,httpStatus,finishReason,attempts,durationMs,promptTokens,completionTokens,totalTokens,reasoningTokens,extractedLen,extractedSha256");
        foreach (var c in run.Cases)
        {
            sb.Append(Csv(c.CaseId)).Append(',');
            sb.Append(Csv(c.FileName)).Append(',');
            sb.Append(c.CasePassed).Append(',');
            sb.Append(Csv(c.Outcome)).Append(',');
            sb.Append(Csv(c.FailureCategory)).Append(',');
            sb.Append(c.DeterministicPassed).Append(',');
            sb.Append(c.ModelOutputJsonValid).Append(',');
            sb.Append(Csv(c.Judge.FailureReason ?? "")).Append(',');
            sb.Append(c.Judge.Groundedness).Append(',');
            sb.Append(c.Judge.RequiredFactCoverage).Append(',');
            sb.Append(c.Judge.AttachmentFocus).Append(',');
            sb.Append(c.Judge.ReviewerUsefulness).Append(',');
            sb.Append(c.Judge.Hallucination).Append(',');
            sb.Append(c.Judge.ForbiddenClaim).Append(',');
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
            sb.Append(c.ExtractedTextSha256).AppendLine();
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
        sb.AppendLine($"- Candidate: `{run.CandidateProvider}/{run.CandidateProfile}`");
        sb.AppendLine($"- Judge deployment: `{run.JudgeDeployment}`");
        sb.AppendLine();
        sb.AppendLine($"- Total: {run.Total}");
        sb.AppendLine($"- Passed: {run.Passed}");
        sb.AppendLine($"- Pass rate: {run.PassRate.ToString("P1", CultureInfo.InvariantCulture)}");
        sb.AppendLine($"- Mean rubric: {run.MeanRubric.ToString("F2", CultureInfo.InvariantCulture)}");
        sb.AppendLine();
        sb.AppendLine("| Case | Pass | Outcome | Det | JSON | Empty | G | RF | AF | RU | Hall | Forb |");
        sb.AppendLine("|------|------|---------|-----|------|-------|---|----|----|----|------|------|");
        foreach (var c in run.Cases)
        {
            sb.Append("| ").Append(c.CaseId)
              .Append(" | ").Append(c.CasePassed ? "OK" : "FAIL")
              .Append(" | ").Append(c.Outcome)
              .Append(" | ").Append(c.DeterministicPassed ? "OK" : "FAIL")
              .Append(" | ").Append(c.ModelOutputJsonValid ? "Y" : "N")
              .Append(" | ").Append(c.ExtractionStoppedOnEmpty ? "Y" : "N")
              .Append(" | ").Append(c.Judge.Groundedness)
              .Append(" | ").Append(c.Judge.RequiredFactCoverage)
              .Append(" | ").Append(c.Judge.AttachmentFocus)
              .Append(" | ").Append(c.Judge.ReviewerUsefulness)
              .Append(" | ").Append(c.Judge.Hallucination ? "Y" : "N")
              .Append(" | ").Append(c.Judge.ForbiddenClaim ? "Y" : "N")
              .AppendLine(" |");
        }
        return sb.ToString();
    }
}
