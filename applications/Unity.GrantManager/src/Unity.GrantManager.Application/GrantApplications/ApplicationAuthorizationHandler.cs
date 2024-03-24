using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Unity.GrantManager.Applications;
using Volo.Abp.Authorization.Permissions;
using Unity.GrantManager.Permissions;

namespace Unity.GrantManager.GrantApplications;

public class ApplicationAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement, Application>, ISingletonDependency
{
    protected IPermissionChecker PermissionChecker { get; }

    public ApplicationAuthorizationHandler(IPermissionChecker permissionChecker)
    {
        PermissionChecker = permissionChecker;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        Application resource)
    {
        // Approve and Deny button is only for Approvers
        if ((requirement.Name.Equals(GrantApplicationAction.Approve.ToString()) || requirement.Name.Equals(GrantApplicationAction.Deny.ToString()))
            && !await PermissionChecker.IsGrantedAsync(GrantApplicationPermissions.Approvals.Complete))
        {
            context.Fail();
            return;
        }

        // After approval, Close and Withdraw button is only for Approvers
        if(resource.ApplicationStatus.StatusCode == GrantApplicationState.GRANT_APPROVED && 
            ((requirement.Name.Equals(GrantApplicationAction.Close.ToString()) || requirement.Name.Equals(GrantApplicationAction.Withdraw.ToString())) &&
            !await PermissionChecker.IsGrantedAsync(GrantApplicationPermissions.Approvals.Complete)))
        {
            context.Fail();
            return;
        }

        // After application approval, decline, or withdraw -- close button is enabled if you are an Approver
        if (requirement.Name.Equals(GrantApplicationAction.Close.ToString()) && 
            (resource.ApplicationStatus.StatusCode == GrantApplicationState.GRANT_APPROVED
            || resource.ApplicationStatus.StatusCode == GrantApplicationState.GRANT_NOT_APPROVED
            || resource.ApplicationStatus.StatusCode == GrantApplicationState.WITHDRAWN)
            && !await PermissionChecker.IsGrantedAsync(GrantApplicationPermissions.Approvals.Complete))
        {
            context.Fail();
            return;
        }

        context.Succeed(requirement);
    }
}
