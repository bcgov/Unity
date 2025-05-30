﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Unity.Modules.Shared;
using Volo.Abp;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Assessments;
public class AssessmentAuthorizationHandler : AuthorizationHandler<OperationAuthorizationRequirement, Assessment>, ISingletonDependency
{
    protected IPermissionChecker PermissionChecker { get; }

    public AssessmentAuthorizationHandler(IPermissionChecker permissionChecker)
    {
        PermissionChecker = permissionChecker;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        Assessment resource)
    {
        if (requirement.Name.Equals(UnitySelector.Review.AssessmentReviewList.Update.SendBack)
            && await CheckPolicyAsync(requirement.Name, context))
        {
            context.Succeed(requirement);
            return;
        }

        // Complete only if Assessor/Reviewer and current user
        if (requirement.Name.Equals(UnitySelector.Review.AssessmentReviewList.Update.Complete)
            && await HasConfirmPermissionAsync(context, resource))
        {
            context.Succeed(requirement);
            return;
        }

        context.Fail();
    }

    protected virtual async Task<bool> HasConfirmPermissionAsync(AuthorizationHandlerContext context, Assessment resource)
    {
        // NOTE: This should be replaced by mapping a claim of the userId to 'sub'
        var currentUserId = FindUserIdirId(context.User);
        if (currentUserId is null)
        {
            return false;
        }

        // Only the assigned user or the creator of the assessment can act
        if (resource.AssessorId == currentUserId || resource.CreatorId == currentUserId)
        {
            return await PermissionChecker.IsGrantedAsync(context.User,
                   UnitySelector.Review.AssessmentReviewList.Update.Complete);
        }

        return false;
    }

    protected virtual async Task<bool> CheckPolicyAsync(string permissionName, AuthorizationHandlerContext context)
    {
        if (!permissionName.IsNullOrEmpty() && await PermissionChecker.IsGrantedAsync(permissionName))
        {
            return true;
        }

        return false;
    }

    public static Guid? FindUserIdirId(ClaimsPrincipal principal)
    {
        Check.NotNull(principal, nameof(principal));

        var userIdOrNull = principal.Claims?.FirstOrDefault(c => c.Type == "UserId");
        if (userIdOrNull == null || userIdOrNull.Value.IsNullOrWhiteSpace())
        {
            return null;
        }

        if (Guid.TryParse(userIdOrNull.Value, out Guid guid))
        {
            return guid;
        }

        return null;
    }
}
