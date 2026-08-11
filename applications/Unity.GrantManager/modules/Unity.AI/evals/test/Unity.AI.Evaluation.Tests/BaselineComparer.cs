using System.Collections.Generic;
using System.Linq;

namespace Unity.AI.Evaluation;

public sealed record BaselineComparison(
    bool Regressed,
    IReadOnlyList<string> Reasons);

internal static class BaselineComparer
{
    private const double DropThreshold = 0.03; // 3 percentage points

    // Rules from plan §10:
    //  - dataset hash mismatch => hard fail
    //  - pass-rate drop > 3pp => fail
    //  - normalized mean rubric drop > 3pp (of 5.0) => fail
    //  - any previously-safe case now has a blocking unsupported or forbidden claim => fail
    //  - retry-once + reproduce is handled by the caller (this is a pure compare)
    public static BaselineComparison Compare(Baseline baseline, EvalRun run)
    {
        var reasons = new List<string>();

        if (baseline.BaselineVersion != 2)
        {
            reasons.Add($"baseline_version_mismatch:baseline={baseline.BaselineVersion};required=2");
            return new BaselineComparison(true, reasons);
        }

        if (!string.Equals(baseline.DatasetHash, run.DatasetHash, System.StringComparison.Ordinal))
        {
            reasons.Add($"dataset_hash_mismatch:baseline={baseline.DatasetHash};run={run.DatasetHash}");
            return new BaselineComparison(true, reasons);
        }

        if (!string.Equals(baseline.HarnessVersion, run.HarnessVersion, System.StringComparison.Ordinal))
        {
            reasons.Add(
                $"harness_version_mismatch:baseline={baseline.HarnessVersion};run={run.HarnessVersion}");
            return new BaselineComparison(true, reasons);
        }

        if (!string.Equals(baseline.CaseSetHash, run.CaseSetHash, System.StringComparison.Ordinal)
            || !baseline.Cases.Keys.OrderBy(id => id, System.StringComparer.Ordinal)
                .SequenceEqual(run.Cases.Select(result => result.CaseId)
                    .OrderBy(id => id, System.StringComparer.Ordinal)))
        {
            reasons.Add($"case_set_mismatch:baseline={baseline.CaseSetHash};run={run.CaseSetHash}");
            return new BaselineComparison(true, reasons);
        }

        if (!string.Equals(
                baseline.Candidate.Provider,
                run.CandidateProvider,
                System.StringComparison.OrdinalIgnoreCase)
            || !string.Equals(
                baseline.Candidate.Profile,
                run.CandidateProfile,
                System.StringComparison.OrdinalIgnoreCase))
        {
            reasons.Add(
                $"candidate_mismatch:baseline={baseline.Candidate.Provider}/{baseline.Candidate.Profile};" +
                $"run={run.CandidateProvider}/{run.CandidateProfile}");
            return new BaselineComparison(true, reasons);
        }

        if (!baseline.Candidate.Models.OrderBy(value => value, System.StringComparer.Ordinal)
                .SequenceEqual(run.CandidateModels.OrderBy(value => value, System.StringComparer.Ordinal))
            || !baseline.Candidate.PromptTemplateHashes.OrderBy(value => value, System.StringComparer.Ordinal)
                .SequenceEqual(run.PromptTemplateHashes.OrderBy(value => value, System.StringComparer.Ordinal)))
        {
            reasons.Add("candidate_snapshot_mismatch");
            return new BaselineComparison(true, reasons);
        }

        if (!string.Equals(baseline.Judge.Deployment, run.JudgeDeployment, System.StringComparison.Ordinal)
            || !string.Equals(baseline.Judge.ApiVersion, run.JudgeApiVersion, System.StringComparison.Ordinal)
            || !baseline.Judge.Models.OrderBy(value => value, System.StringComparer.Ordinal)
                .SequenceEqual(run.JudgeModels.OrderBy(value => value, System.StringComparer.Ordinal)))
        {
            reasons.Add("judge_snapshot_mismatch");
            return new BaselineComparison(true, reasons);
        }

        var promptVersions = run.Cases
            .Select(result => result.EffectivePromptVersion)
            .Where(version => !string.Equals(
                version,
                "not-invoked",
                System.StringComparison.Ordinal))
            .Distinct(System.StringComparer.Ordinal)
            .ToList();
        if (promptVersions.Count != 1
            || !string.Equals(
                baseline.Candidate.PromptVersion,
                promptVersions[0],
                System.StringComparison.Ordinal))
        {
            reasons.Add(
                $"prompt_version_mismatch:baseline={baseline.Candidate.PromptVersion};" +
                $"run={string.Join("+", promptVersions)}");
            return new BaselineComparison(true, reasons);
        }

        var passDrop = baseline.Aggregate.PassRate - run.PassRate;
        if (passDrop > DropThreshold)
        {
            reasons.Add($"pass_rate_drop={passDrop:F3}");
        }

        var rubricDropNormalized = (baseline.Aggregate.MeanRubric - run.MeanRubric) / 5.0;
        if (rubricDropNormalized > DropThreshold)
        {
            reasons.Add($"mean_rubric_drop_normalized={rubricDropNormalized:F3}");
        }

        var factCoverageDrop = baseline.Aggregate.FactCoverageRate - run.FactCoverageRate;
        if (factCoverageDrop > DropThreshold)
        {
            reasons.Add($"fact_coverage_drop={factCoverageDrop:F3}");
        }

        foreach (var caseResult in run.Cases)
        {
            if (!baseline.Cases.TryGetValue(caseResult.CaseId, out var prior))
            {
                continue; // new case; not a regression
            }

            if (prior.Pass && caseResult.Judge.HasBlockingUnsupportedClaim)
            {
                reasons.Add($"new_blocking_unsupported_claim:{caseResult.CaseId}");
            }
            if (!prior.ForbiddenClaim && caseResult.Judge.ForbiddenClaim)
            {
                reasons.Add($"new_forbidden_claim:{caseResult.CaseId}");
            }
        }

        return new BaselineComparison(reasons.Count > 0, reasons);
    }
}
