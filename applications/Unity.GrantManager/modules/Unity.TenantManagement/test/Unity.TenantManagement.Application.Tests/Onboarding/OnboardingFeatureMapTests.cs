using Shouldly;
using Xunit;

namespace Unity.TenantManagement.Onboarding;

public class OnboardingFeatureMapTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ResolveFeatureKeys_BlankInput_ReturnsEmpty(string? input)
    {
        var result = OnboardingFeatureMap.ResolveFeatureKeys(input!);

        result.ShouldBeEmpty();
    }

    [Fact]
    public void ResolveFeatureKeys_UnrecognizedTokens_AreDroppedNotPassedThrough()
    {
        // The "Features" worksheet field is filled out by the applicant submitting the onboarding
        // request, so unrecognized/arbitrary tokens must never reach the real ABP feature name list.
        var result = OnboardingFeatureMap.ResolveFeatureKeys("Payments,SomeArbitraryFeatureKey,DROP TABLE Tenants");

        result.ShouldBe(["Unity.Payments"]);
    }

    [Theory]
    [InlineData("Payments,Reporting")]
    [InlineData("Payments;Reporting")]
    [InlineData("Payments|Reporting")]
    public void ResolveFeatureKeys_AllDelimiterVariants_AreSupported(string input)
    {
        var result = OnboardingFeatureMap.ResolveFeatureKeys(input);

        result.ShouldBe(["Unity.Payments", "Unity.Reporting"]);
    }

    [Fact]
    public void ResolveFeatureKeys_KeyMatchingIsCaseInsensitive()
    {
        var result = OnboardingFeatureMap.ResolveFeatureKeys("payments,AIREPORTING");

        result.ShouldBe(["Unity.Payments", "Unity.AIReporting"]);
    }

    [Fact]
    public void ResolveFeatureKeys_DuplicateTokens_CollapseToASingleKey()
    {
        var result = OnboardingFeatureMap.ResolveFeatureKeys("Payments,Payments,Flex");

        result.ShouldBe(["Unity.Payments", "Unity.Flex"]);
    }

    [Fact]
    public void ResolveFeatureKeys_CheckboxGroupJsonFormat_OnlyEnabledKeysAreResolved()
    {
        var json = """[{"key":"aiReporting","value":true},{"key":"aiScoring","value":false}]""";

        var result = OnboardingFeatureMap.ResolveFeatureKeys(json);

        result.ShouldBe(["Unity.AIReporting"]);
    }

    [Fact]
    public void ResolveFeatureKeys_MalformedJsonArray_DoesNotThrowAndReturnsEmpty()
    {
        var result = OnboardingFeatureMap.ResolveFeatureKeys("[ this is not valid json");

        result.ShouldBeEmpty();
    }
}
