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
    public async Task Should_Permit_Defer_From_OpenOnboardingStates(GrantApplicationState state)
    {
        var allowed = await OnboardingApplicationManager.IsActionAllowed(
            ApplicationIn(state), GrantApplicationAction.Defer);

        allowed.ShouldBeTrue();
    }

    [Theory]
    [InlineData(GrantApplicationAction.Submit, true)]
    [InlineData(GrantApplicationAction.Close, true)]
    [InlineData(GrantApplicationAction.Approve, false)]
    [InlineData(GrantApplicationAction.Deny, false)]
    [InlineData(GrantApplicationAction.Defer, false)]
    public async Task Should_Only_Permit_Submit_Or_Close_From_Defer(GrantApplicationAction action, bool expected)
    {
        var allowed = await OnboardingApplicationManager.IsActionAllowed(
            ApplicationIn(GrantApplicationState.DEFER), action);

        allowed.ShouldBe(expected);
    }

    [Theory]
    [InlineData(GrantApplicationAction.Submit, true)]
    [InlineData(GrantApplicationAction.Defer, false)]
    [InlineData(GrantApplicationAction.Approve, false)]
    [InlineData(GrantApplicationAction.Deny, false)]
    [InlineData(GrantApplicationAction.Close, false)]
    public async Task Should_Only_Permit_Submit_From_Closed(GrantApplicationAction action, bool expected)
    {
        var allowed = await OnboardingApplicationManager.IsActionAllowed(
            ApplicationIn(GrantApplicationState.CLOSED), action);

        allowed.ShouldBe(expected);
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
    public async Task Should_Match_Decision_Transitions(
        GrantApplicationState state, GrantApplicationAction action, bool expected)
    {
        var allowed = await OnboardingApplicationManager.IsActionAllowed(ApplicationIn(state), action);

        allowed.ShouldBe(expected);
    }
}
