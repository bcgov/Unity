using Shouldly;
using Xunit;

namespace Unity.AI.Evaluation;

[Trait("Category", "AIEvalOffline")]
public class EvalCasePassPolicyTests
{
    [Theory]
    [InlineData("none", false, true)]
    [InlineData("minor", true, true)]
    [InlineData("material", true, false)]
    [InlineData("critical", true, false)]
    [InlineData("unexpected", true, false)]
    public void Should_Only_Block_NonMinor_Unsupported_Claims(
        string severity,
        bool hallucination,
        bool expectedPass)
    {
        var deterministic = new DeterministicCheckResult(true, [], true);
        var verdict = CreateVerdict(hallucination, severity);

        EvalCasePassPolicy.Passes(deterministic, verdict).ShouldBe(expectedPass);
    }

    [Fact]
    public void Should_Keep_Minor_Unsupported_Claim_As_Warning()
    {
        var verdict = CreateVerdict(hallucination: true, severity: "minor");

        verdict.HasMinorUnsupportedClaimWarning.ShouldBeTrue();
        verdict.HasBlockingUnsupportedClaim.ShouldBeFalse();
    }

    [Fact]
    public void Should_Fail_When_Extraction_Produced_No_Text()
    {
        var deterministic = new DeterministicCheckResult(true, [], false);
        var verdict = JudgeVerdict.Skip("no extractable text");

        EvalCasePassPolicy.Passes(
                deterministic,
                verdict,
                extractionStoppedOnEmpty: true)
            .ShouldBeFalse();
    }

    private static JudgeVerdict CreateVerdict(bool hallucination, string severity) =>
        new(
            Groundedness: 4,
            RequiredFactCoverage: 4,
            AttachmentFocus: 4,
            ReviewerUsefulness: 4,
            Hallucination: hallucination,
            ForbiddenClaim: false,
            FailureReason: null)
        {
            HallucinationSeverity = severity,
            UnsupportedClaimCount = hallucination ? 1 : 0,
        };
}
