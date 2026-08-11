using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Unity.AI.Runtime;

namespace Unity.AI.Evaluation;

public sealed record DeterministicCheckResult(
    bool Passed,
    IReadOnlyList<string> Failures,
    bool ModelOutputJsonValid);

// Deterministic checks per case. All must pass for the case to pass the pipeline;
// judge rubric is evaluated separately.
internal static class DeterministicChecks
{
    private static readonly Regex SentenceSplit = new(@"[.!?](?:\s|$)", RegexOptions.Compiled);

    public static DeterministicCheckResult Run(
        EvalCase evalCase,
        string summary,
        string modelOutput,
        AIOperationOutcome outcome)
    {
        var failures = new List<string>();

        if (outcome != AIOperationOutcome.Success)
        {
            failures.Add($"outcome={outcome}");
        }

        if (string.IsNullOrWhiteSpace(summary))
        {
            failures.Add("empty_summary");
        }

        var modelOutputJsonValid = IsSummaryJsonShape(modelOutput);
        if (!modelOutputJsonValid)
        {
            // Record-only per plan §7; do NOT fail production behavior on this in the aggregate.
            // Kept as a metric string so it shows in the report but does not block the case pass rule.
        }

        var sentences = CountSentences(summary);
        if (sentences < 1 || sentences > 2)
        {
            failures.Add($"sentence_count={sentences}");
        }

        // JSONL fixtures define literal forbidden phrases. CSV cases define
        // semantic instructions (for example, "Do not claim...") that cannot
        // be meaningfully checked with substring matching and are evaluated by
        // the structured judge trap assessments instead.
        if (string.Equals(evalCase.Source, "jsonl", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var forbidden in evalCase.ForbiddenClaims)
            {
                if (!string.IsNullOrWhiteSpace(forbidden) &&
                    summary.Contains(forbidden, StringComparison.OrdinalIgnoreCase))
                {
                    failures.Add($"forbidden_claim:{forbidden}");
                }
            }
        }

        return new DeterministicCheckResult(
            failures.Count == 0,
            failures,
            modelOutputJsonValid);
    }

    private static bool IsSummaryJsonShape(string modelOutput)
    {
        if (string.IsNullOrWhiteSpace(modelOutput))
        {
            return false;
        }
        try
        {
            using var doc = JsonDocument.Parse(modelOutput);
            return doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty("summary", out var s)
                && s.ValueKind == JsonValueKind.String;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static int CountSentences(string summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
        {
            return 0;
        }
        return SentenceSplit.Matches(summary.Trim()).Count(m => m.Index > 0);
    }
}
