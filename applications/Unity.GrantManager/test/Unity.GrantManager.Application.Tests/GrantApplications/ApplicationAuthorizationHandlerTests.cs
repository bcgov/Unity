using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using NSubstitute;
using Shouldly;
using System.Security.Claims;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Permissions;
using Volo.Abp.Authorization.Permissions;
using Xunit;

namespace Unity.GrantManager.GrantApplications;

public class ApplicationAuthorizationHandlerTests
{
    private readonly IPermissionChecker _permissions = Substitute.For<IPermissionChecker>();

    [Theory]
    [InlineData(GrantApplicationAction.StartReview, GrantApplicationPermissions.Reviews.StartInitial)]
    [InlineData(GrantApplicationAction.CompleteReview, GrantApplicationPermissions.Reviews.CompleteInitial)]
    [InlineData(GrantApplicationAction.StartAssessment, GrantApplicationPermissions.Assessments.Start)]
    [InlineData(GrantApplicationAction.Internal_StartAssessment, GrantApplicationPermissions.Assessments.Start)]
    [InlineData(GrantApplicationAction.CompleteAssessment, GrantApplicationPermissions.Assessments.Complete)]
    public async Task Should_Require_The_Corresponding_Workflow_Permission(GrantApplicationAction action, string permission)
    {
        var denied = await AuthorizeAsync(action);
        denied.HasFailed.ShouldBeTrue();

        _permissions.IsGrantedAsync(permission).Returns(true);

        var allowed = await AuthorizeAsync(action);
        allowed.HasSucceeded.ShouldBeTrue();
    }

    [Theory]
    [InlineData(GrantApplicationAction.StartReview, GrantApplicationPermissions.Reviews.CompleteInitial)]
    [InlineData(GrantApplicationAction.CompleteReview, GrantApplicationPermissions.Reviews.StartInitial)]
    [InlineData(GrantApplicationAction.StartReview, GrantApplicationPermissions.StatusWorkflow.Default)]
    [InlineData(GrantApplicationAction.CompleteReview, GrantApplicationPermissions.StatusWorkflow.Default)]
    [InlineData(GrantApplicationAction.StartReview, GrantApplicationPermissions.Assessments.Start)]
    [InlineData(GrantApplicationAction.CompleteReview, GrantApplicationPermissions.Assessments.Complete)]
    [InlineData(GrantApplicationAction.StartAssessment, GrantApplicationPermissions.Assessments.Complete)]
    [InlineData(GrantApplicationAction.CompleteAssessment, GrantApplicationPermissions.Assessments.Start)]
    [InlineData(GrantApplicationAction.StartAssessment, GrantApplicationPermissions.StatusWorkflow.Default)]
    [InlineData(GrantApplicationAction.CompleteAssessment, GrantApplicationPermissions.StatusWorkflow.Default)]
    public async Task Should_Not_Authorize_Action_With_Another_Workflow_Permission(GrantApplicationAction action, string permission)
    {
        _permissions.IsGrantedAsync(permission).Returns(true);

        (await AuthorizeAsync(action)).HasFailed.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Preserve_Internal_Assignment_Authorization()
    {
        (await AuthorizeAsync(GrantApplicationAction.Internal_Assign)).HasSucceeded.ShouldBeTrue();
    }

    [Theory]
    [InlineData(GrantApplicationAction.Approve)]
    [InlineData(GrantApplicationAction.Deny)]
    public async Task Should_Keep_Approval_And_Decline_On_The_Existing_Permission(GrantApplicationAction action)
    {
        (await AuthorizeAsync(action)).HasFailed.ShouldBeTrue();

        _permissions.IsGrantedAsync(GrantApplicationPermissions.Approvals.Complete).Returns(true);

        (await AuthorizeAsync(action)).HasSucceeded.ShouldBeTrue();
    }

    [Theory]
    [InlineData(GrantApplicationAction.Close, GrantApplicationState.GRANT_APPROVED)]
    [InlineData(GrantApplicationAction.Withdraw, GrantApplicationState.GRANT_APPROVED)]
    [InlineData(GrantApplicationAction.Close, GrantApplicationState.GRANT_NOT_APPROVED)]
    [InlineData(GrantApplicationAction.Close, GrantApplicationState.WITHDRAWN)]
    public async Task Should_Preserve_Final_State_Approval_Restrictions(GrantApplicationAction action, GrantApplicationState state)
    {
        (await AuthorizeAsync(action, state)).HasFailed.ShouldBeTrue();

        _permissions.IsGrantedAsync(GrantApplicationPermissions.Approvals.Complete).Returns(true);

        (await AuthorizeAsync(action, state)).HasSucceeded.ShouldBeTrue();
    }

    private async Task<AuthorizationHandlerContext> AuthorizeAsync(
        GrantApplicationAction action, GrantApplicationState state = GrantApplicationState.UNDER_ASSESSMENT)
    {
        var requirement = new OperationAuthorizationRequirement { Name = action.ToString() };
        var application = new Application { ApplicationStatus = new ApplicationStatus { StatusCode = state } };
        var context = new AuthorizationHandlerContext([requirement], new ClaimsPrincipal(), application);
        await new ApplicationAuthorizationHandler(_permissions).HandleAsync(context);
        return context;
    }
}
