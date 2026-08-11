using Shouldly;
using System.Text.Json;
using Xunit;

namespace Unity.AI.Evaluation;

[Trait("Category", "AIEvalOffline")]
public class AzureOpenAIJudgeClientTests
{
    [Fact]
    public void Should_Derive_Public_Metrics_From_Structured_Assessments()
    {
        var content = JsonSerializer.Serialize(new
        {
            groundedness = 3,
            attachmentFocus = 5,
            reviewerUsefulness = 4,
            rationale = "One material unsupported claim; one fact is partial.",
            factAssessments = new object[]
            {
                new { factId = "f1", coverage = "covered", rationale = "Present." },
                new { factId = "f2", coverage = "partial", rationale = "Partly present." },
            },
            claimAssessments = new object[]
            {
                new { claim = "Supported claim", support = "supported", evidence = "Source text", severity = "none" },
                new { claim = "Ambiguous claim", support = "ambiguous", evidence = "Evidence is unclear", severity = "minor" },
                new { claim = "Unsupported claim", support = "unsupported", evidence = "Not in source", severity = "material" },
            },
            trapAssessments = new object[]
            {
                new { trapId = "t1", triggered = false, rationale = "No prohibited approval claim." },
            },
        });
        var body = JsonSerializer.Serialize(new
        {
            model = "judge-model-2026-01-01",
            choices = new[] { new { message = new { content } } },
            usage = new
            {
                prompt_tokens = 1200,
                completion_tokens = 300,
                total_tokens = 1500,
                completion_tokens_details = new { reasoning_tokens = 80 },
            },
        });

        var parsed = AzureOpenAIJudgeClient.TryExtractVerdict(
            body,
            new[] { "f1", "f2" },
            new[] { "t1" },
            out var verdict);

        parsed.ShouldBeTrue();
        verdict.FactCoverageRate.ShouldBe(0.75);
        verdict.RequiredFactCoverage.ShouldBe(3);
        verdict.CoveredFactCount.ShouldBe(1);
        verdict.PartialFactIds.ShouldContain("f2");
        verdict.Hallucination.ShouldBeTrue();
        verdict.UnsupportedClaimCount.ShouldBe(1);
        verdict.HallucinationSeverity.ShouldBe("material");
        verdict.ForbiddenClaim.ShouldBeFalse();
        verdict.JudgeModel.ShouldBe("judge-model-2026-01-01");
        verdict.JudgePromptTokens.ShouldBe(1200);
        verdict.JudgeCompletionTokens.ShouldBe(300);
        verdict.JudgeTotalTokens.ShouldBe(1500);
        verdict.JudgeReasoningTokens.ShouldBe(80);
        verdict.Audit.ShouldNotBeNull();
    }

    [Fact]
    public void Should_Default_Usage_To_Zero_When_Missing()
    {
        var content = JsonSerializer.Serialize(new
        {
            groundedness = 5,
            attachmentFocus = 5,
            reviewerUsefulness = 5,
            rationale = "All good.",
            factAssessments = System.Array.Empty<object>(),
            claimAssessments = new object[]
            {
                new { claim = "Supported claim", support = "supported", evidence = "Source text", severity = "none" },
            },
            trapAssessments = System.Array.Empty<object>(),
        });
        var body = JsonSerializer.Serialize(new
        {
            model = "judge-model-2026-01-01",
            choices = new[] { new { message = new { content } } },
        });

        AzureOpenAIJudgeClient.TryExtractVerdict(
                body,
                System.Array.Empty<string>(),
                System.Array.Empty<string>(),
                out var verdict)
            .ShouldBeTrue();

        verdict.JudgePromptTokens.ShouldBe(0);
        verdict.JudgeCompletionTokens.ShouldBe(0);
        verdict.JudgeTotalTokens.ShouldBe(0);
        verdict.JudgeReasoningTokens.ShouldBe(0);
    }

    [Fact]
    public void Should_Reject_Missing_Fact_Assessment()
    {
        var content = JsonSerializer.Serialize(new
        {
            groundedness = 5,
            attachmentFocus = 5,
            reviewerUsefulness = 5,
            rationale = "Incomplete fact assessment set.",
            factAssessments = new object[]
            {
                new { factId = "f1", coverage = "covered", rationale = "Present." },
            },
            claimAssessments = new object[]
            {
                new { claim = "Supported claim", support = "supported", evidence = "Source text", severity = "none" },
            },
            trapAssessments = System.Array.Empty<object>(),
        });
        var body = JsonSerializer.Serialize(new
        {
            model = "judge-model-2026-01-01",
            choices = new[] { new { message = new { content } } },
            usage = new
            {
                prompt_tokens = 100,
                completion_tokens = 20,
                total_tokens = 120,
                completion_tokens_details = new { reasoning_tokens = 10 },
            },
        });

        var parsed = AzureOpenAIJudgeClient.TryExtractVerdict(
                body,
                new[] { "f1", "f2" },
                System.Array.Empty<string>(),
                out _,
                out var metadata);

        parsed.ShouldBeFalse();
        metadata.ValidationFailure.ShouldBe("invalid fact assessments");
        metadata.Usage.Total.ShouldBe(120);
        metadata.Usage.Reasoning.ShouldBe(10);
    }
}
