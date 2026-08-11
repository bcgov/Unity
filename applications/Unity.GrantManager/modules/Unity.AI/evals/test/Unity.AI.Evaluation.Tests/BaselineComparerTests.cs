using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace Unity.AI.Evaluation;

[Trait("Category", "AIEvalOffline")]
public class BaselineComparerTests
{
    [Fact]
    public void Should_Accept_Equivalent_Run()
    {
        var run = CreateRunForTests(hallucination: false);
        var comparison = BaselineComparer.Compare(CreateBaseline(run), run);

        comparison.Regressed.ShouldBeFalse();
        comparison.Reasons.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Reject_New_Blocking_Unsupported_Claim()
    {
        var baselineRun = CreateRunForTests(hallucination: false);
        var currentRun = CreateRunForTests(hallucination: true);

        var comparison = BaselineComparer.Compare(CreateBaseline(baselineRun), currentRun);

        comparison.Regressed.ShouldBeTrue();
        comparison.Reasons.ShouldContain("new_blocking_unsupported_claim:case-1");
    }

    [Fact]
    public void Should_Not_Reject_New_Minor_Unsupported_Claim()
    {
        var baselineRun = CreateRunForTests(hallucination: false);
        var currentRun = CreateRunForTests(hallucination: true);
        var currentCase = currentRun.Cases[0] with
        {
            Judge = currentRun.Cases[0].Judge with { HallucinationSeverity = "minor" },
            CasePassed = true,
        };
        currentRun = currentRun with { Cases = new[] { currentCase } };

        var comparison = BaselineComparer.Compare(CreateBaseline(baselineRun), currentRun);

        comparison.Regressed.ShouldBeFalse();
        comparison.Reasons.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Reject_Candidate_Mismatch()
    {
        var run = CreateRunForTests(hallucination: false);
        var baseline = CreateBaseline(run);
        baseline.Candidate.Profile = "different-profile";

        var comparison = BaselineComparer.Compare(baseline, run);

        comparison.Regressed.ShouldBeTrue();
        comparison.Reasons.Count.ShouldBe(1);
        comparison.Reasons[0].ShouldStartWith("candidate_mismatch:");
    }

    [Fact]
    public void Should_Reject_Case_Set_Mismatch()
    {
        var run = CreateRunForTests(hallucination: false);
        var baseline = CreateBaseline(run);
        baseline.CaseSetHash = "sha256:different";

        var comparison = BaselineComparer.Compare(baseline, run);

        comparison.Regressed.ShouldBeTrue();
        comparison.Reasons.ShouldContain(reason => reason.StartsWith("case_set_mismatch:"));
    }

    [Fact]
    public void Should_Reject_Model_Snapshot_Mismatch()
    {
        var run = CreateRunForTests(hallucination: false);
        var baseline = CreateBaseline(run);
        baseline.Candidate.Models = new List<string> { "different-model" };

        var comparison = BaselineComparer.Compare(baseline, run);

        comparison.Regressed.ShouldBeTrue();
        comparison.Reasons.ShouldContain("candidate_snapshot_mismatch");
    }

    internal static EvalRun CreateRunForTests(bool hallucination)
    {
        var verdict = new JudgeVerdict(
            Groundedness: 4,
            RequiredFactCoverage: 4,
            AttachmentFocus: 4,
            ReviewerUsefulness: 4,
            Hallucination: hallucination,
            ForbiddenClaim: false,
            FailureReason: null)
        {
            FactCoverageRate = 1,
            CoveredFactCount = 3,
            HallucinationSeverity = hallucination ? "material" : "none",
            UnsupportedClaimCount = hallucination ? 1 : 0,
            JudgeModel = "judge-model",
        };
        var result = new CaseResult(
            CaseId: "case-1",
            FileName: "case.txt",
            Tags: new[] { "synthetic" },
            DeterministicPassed: true,
            DeterministicFailures: System.Array.Empty<string>(),
            ModelOutputJsonValid: true,
            Judge: verdict,
            CasePassed: !hallucination,
            Outcome: "Success",
            FailureCategory: "None",
            Model: "model",
            ProviderName: "OpenAI",
            ProfileName: "Gpt5Mini",
            EffectivePromptVersion: "v1",
            HttpStatusCode: 200,
            FinishReason: "stop",
            AttemptCount: 1,
            DurationMs: 10,
            PromptTokensTotal: 10,
            CompletionTokensTotal: 5,
            TotalTokensTotal: 15,
            ReasoningTokensTotal: 0,
            ExtractionStoppedOnEmpty: false,
            ExtractedTextLength: 20,
            ExtractedTextSha256: "hash")
        {
            PromptTemplateSha256 = "sha256:prompt",
        };

        return new EvalRun(
            HarnessVersion: "0.2.0",
            CommitSha: "commit",
            DatasetHash: "sha256:dataset",
            UtcTimestamp: "timestamp",
            JudgeDeployment: "judge",
            CandidateProvider: "OpenAI",
            CandidateProfile: "Gpt5Mini",
            Cases: new[] { result })
        {
            CaseSetHash = "sha256:case-set",
            JudgeApiVersion = "2025-01-01",
            SourceFilter = "csv",
            AvailableCaseCount = 1,
            FullCaseSet = true,
            CandidateModels = new[] { "model" },
            JudgeModels = new[] { "judge-model" },
            PromptTemplateHashes = new[] { "sha256:prompt" },
        };
    }

    private static Baseline CreateBaseline(EvalRun run)
    {
        var result = run.Cases[0];
        return new Baseline
        {
            DatasetHash = run.DatasetHash,
            HarnessVersion = run.HarnessVersion,
            CaseSetHash = run.CaseSetHash,
            Candidate = new BaselineCandidate
            {
                Provider = run.CandidateProvider,
                Profile = run.CandidateProfile,
                PromptVersion = result.EffectivePromptVersion,
                Models = new List<string>(run.CandidateModels),
                PromptTemplateHashes = new List<string>(run.PromptTemplateHashes),
            },
            Judge = new BaselineJudge
            {
                Deployment = run.JudgeDeployment,
                ApiVersion = run.JudgeApiVersion,
                Models = new List<string>(run.JudgeModels),
            },
            Aggregate = new BaselineAggregate
            {
                PassRate = run.PassRate,
                MeanRubric = run.MeanRubric,
                FactCoverageRate = run.FactCoverageRate,
            },
            Cases = new Dictionary<string, BaselineCase>
            {
                [result.CaseId] = new()
                {
                    Pass = result.CasePassed,
                    Hallucination = result.Judge.Hallucination,
                    ForbiddenClaim = result.Judge.ForbiddenClaim,
                    FactCoverageRate = result.Judge.FactCoverageRate,
                    Rubric = new BaselineRubric
                    {
                        Groundedness = result.Judge.Groundedness,
                        RequiredFactCoverage = result.Judge.RequiredFactCoverage,
                        AttachmentFocus = result.Judge.AttachmentFocus,
                        ReviewerUsefulness = result.Judge.ReviewerUsefulness,
                    },
                },
            },
        };
    }
}
