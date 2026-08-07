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
        var run = CreateRun(hallucination: false);
        var comparison = BaselineComparer.Compare(CreateBaseline(run), run);

        comparison.Regressed.ShouldBeFalse();
        comparison.Reasons.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Reject_New_Hallucination()
    {
        var baselineRun = CreateRun(hallucination: false);
        var currentRun = CreateRun(hallucination: true);

        var comparison = BaselineComparer.Compare(CreateBaseline(baselineRun), currentRun);

        comparison.Regressed.ShouldBeTrue();
        comparison.Reasons.ShouldContain("new_hallucination:case-1");
    }

    [Fact]
    public void Should_Reject_Candidate_Mismatch()
    {
        var run = CreateRun(hallucination: false);
        var baseline = CreateBaseline(run);
        baseline.Candidate.Profile = "different-profile";

        var comparison = BaselineComparer.Compare(baseline, run);

        comparison.Regressed.ShouldBeTrue();
        comparison.Reasons.Count.ShouldBe(1);
        comparison.Reasons[0].ShouldStartWith("candidate_mismatch:");
    }

    private static EvalRun CreateRun(bool hallucination)
    {
        var verdict = new JudgeVerdict(
            Groundedness: 4,
            RequiredFactCoverage: 4,
            AttachmentFocus: 4,
            ReviewerUsefulness: 4,
            Hallucination: hallucination,
            ForbiddenClaim: false,
            FailureReason: null);
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
            ExtractedTextSha256: "hash");

        return new EvalRun(
            HarnessVersion: "0.2.0",
            CommitSha: "commit",
            DatasetHash: "sha256:dataset",
            UtcTimestamp: "timestamp",
            JudgeDeployment: "judge",
            CandidateProvider: "OpenAI",
            CandidateProfile: "Gpt5Mini",
            Cases: new[] { result });
    }

    private static Baseline CreateBaseline(EvalRun run)
    {
        var result = run.Cases[0];
        return new Baseline
        {
            DatasetHash = run.DatasetHash,
            HarnessVersion = run.HarnessVersion,
            Candidate = new BaselineCandidate
            {
                Provider = run.CandidateProvider,
                Profile = run.CandidateProfile,
                PromptVersion = result.EffectivePromptVersion,
            },
            Aggregate = new BaselineAggregate
            {
                PassRate = run.PassRate,
                MeanRubric = run.MeanRubric,
            },
            Cases = new Dictionary<string, BaselineCase>
            {
                [result.CaseId] = new()
                {
                    Pass = result.CasePassed,
                    Hallucination = result.Judge.Hallucination,
                    ForbiddenClaim = result.Judge.ForbiddenClaim,
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
