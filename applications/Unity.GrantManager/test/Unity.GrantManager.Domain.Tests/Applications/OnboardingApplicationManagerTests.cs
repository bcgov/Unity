using System.Threading.Tasks;
using Shouldly;
using Unity.GrantManager.GrantApplications;
using Xunit;

namespace Unity.GrantManager.Applications;

public class OnboardingApplicationManagerTests
{
    private static Application ApplicationIn(GrantApplicationState state) =>
        new()
        {
            ApplicationStatus = new ApplicationStatus { StatusCode = state }
        };

    [Theory]
    [InlineData(GrantApplicationState.SUBMITTED)]
    [InlineData(GrantApplicationState.GRANT_APPROVED)]
    [InlineData(GrantApplicationState.GRANT_NOT_APPROVED)]
    [InlineData(GrantApplicationState.CLOSED)]
    public async Task Should_Permit_Defer_From_EveryOnboardingState(GrantApplicationState state)
    {
        var allowed = await OnboardingApplicationManager.IsActionAllowed(
            ApplicationIn(state), GrantApplicationAction.Defer);

        allowed.ShouldBeTrue();
    }

    [Theory]
    [InlineData(GrantApplicationAction.Submit)]
    [InlineData(GrantApplicationAction.Approve)]
    [InlineData(GrantApplicationAction.Deny)]
    [InlineData(GrantApplicationAction.Close)]
    public async Task Should_Permit_ReturnToAnyState_From_Defer(GrantApplicationAction action)
    {
        var allowed = await OnboardingApplicationManager.IsActionAllowed(
            ApplicationIn(GrantApplicationState.DEFER), action);

        allowed.ShouldBeTrue();
    }

    [Theory]
    [InlineData(GrantApplicationState.SUBMITTED, GrantApplicationAction.Approve, true)]
    [InlineData(GrantApplicationState.SUBMITTED, GrantApplicationAction.Deny, true)]
    [InlineData(GrantApplicationState.SUBMITTED, GrantApplicationAction.Close, false)]
    [InlineData(GrantApplicationState.SUBMITTED, GrantApplicationAction.Submit, false)]
    [InlineData(GrantApplicationState.GRANT_APPROVED, GrantApplicationAction.Close, true)]
    [InlineData(GrantApplicationState.GRANT_APPROVED, GrantApplicationAction.Deny, false)]
    [InlineData(GrantApplicationState.GRANT_NOT_APPROVED, GrantApplicationAction.Close, true)]
    [InlineData(GrantApplicationState.GRANT_NOT_APPROVED, GrantApplicationAction.Approve, false)]
    [InlineData(GrantApplicationState.CLOSED, GrantApplicationAction.Approve, false)]
    [InlineData(GrantApplicationState.CLOSED, GrantApplicationAction.Deny, false)]
    [InlineData(GrantApplicationState.DEFER, GrantApplicationAction.Defer, false)]
    public async Task Should_MatchExistingTransitions_Alongside_Defer(
        GrantApplicationState state, GrantApplicationAction action, bool expected)
    {
        var allowed = await OnboardingApplicationManager.IsActionAllowed(ApplicationIn(state), action);

        allowed.ShouldBe(expected);
    }
}
