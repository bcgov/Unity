using Microsoft.Extensions.DependencyInjection;
using Unity.GrantManager.Permissions;
using Unity.Modules.Shared;
using Unity.Modules.Shared.Permissions;
using Unity.TenantManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;

namespace Unity.GrantManager.Web.Identity.Policy;

internal static class PolicyRegistrant
{
    internal const string PermissionConstant = "Permission";

    internal static void Register(ServiceConfigurationContext context)
    {
        // Using AddAuthorizationBuilder to register authorization services and construct policies
        var authorizationBuilder = context.Services.AddAuthorizationBuilder();

        // Identity Role Policies
        authorizationBuilder.AddPolicy(IdentityPermissions.Roles.Default,
            policy => policy.RequireClaim(PermissionConstant, IdentityPermissions.Roles.Default));
        authorizationBuilder.AddPolicy(IdentityPermissions.Roles.Create,
            policy => policy.RequireClaim(PermissionConstant, IdentityPermissions.Roles.Create));
        authorizationBuilder.AddPolicy(IdentityPermissions.Roles.Update,
            policy => policy.RequireClaim(PermissionConstant, IdentityPermissions.Roles.Update));
        authorizationBuilder.AddPolicy(IdentityPermissions.Roles.Delete,
            policy => policy.RequireClaim(PermissionConstant, IdentityPermissions.Roles.Delete));
        authorizationBuilder.AddPolicy(IdentityPermissions.Roles.ManagePermissions,
            policy => policy.RequireClaim(PermissionConstant, IdentityPermissions.Roles.ManagePermissions));

        // Identity User Policies
        authorizationBuilder.AddPolicy(IdentityPermissions.Users.Default,
            policy => policy.RequireClaim(PermissionConstant, IdentityPermissions.Users.Default));
        authorizationBuilder.AddPolicy(IdentityPermissions.Users.Create,
            policy => policy.RequireClaim(PermissionConstant, IdentityPermissions.Users.Create));
        authorizationBuilder.AddPolicy(IdentityPermissions.Users.Update,
            policy => policy.RequireClaim(PermissionConstant, IdentityPermissions.Users.Update));
        authorizationBuilder.AddPolicy(IdentityPermissions.Users.Delete,
            policy => policy.RequireClaim(PermissionConstant, IdentityPermissions.Users.Delete));
        authorizationBuilder.AddPolicy(IdentityPermissions.Users.ManagePermissions,
            policy => policy.RequireClaim(PermissionConstant, IdentityPermissions.Users.ManagePermissions));

        // User Lookup Policies
        authorizationBuilder.AddPolicy(IdentityPermissions.UserLookup.Default,
            policy => policy.RequireClaim(PermissionConstant, IdentityPermissions.UserLookup.Default));

        // Grant Manager Policies
        authorizationBuilder.AddPolicy(GrantManagerPermissions.Default,
            policy => policy.RequireClaim(PermissionConstant, GrantManagerPermissions.Default));
        authorizationBuilder.AddPolicy(GrantManagerPermissions.Intakes.Default,
            policy => policy.RequireClaim(PermissionConstant, GrantManagerPermissions.Intakes.Default));
        authorizationBuilder.AddPolicy(GrantManagerPermissions.ApplicationForms.Default,
            policy => policy.RequireClaim(PermissionConstant, GrantManagerPermissions.ApplicationForms.Default));

        // Grant Application Policies
        authorizationBuilder.AddPolicy(GrantApplicationPermissions.Applications.Default,
            policy => policy.RequireClaim(PermissionConstant, GrantApplicationPermissions.Applications.Default));
        authorizationBuilder.AddPolicy(GrantApplicationPermissions.Applicants.Default,
            policy => policy.RequireClaim(PermissionConstant, GrantApplicationPermissions.Applicants.Default));
        authorizationBuilder.AddPolicy(GrantApplicationPermissions.Applicants.Edit,
            policy => policy.RequireClaim(PermissionConstant, GrantApplicationPermissions.Applicants.Edit));
        authorizationBuilder.AddPolicy(GrantApplicationPermissions.Applicants.AssignApplicant,
            policy => policy.RequireClaim(PermissionConstant, GrantApplicationPermissions.Applicants.AssignApplicant));
        authorizationBuilder.AddPolicy(GrantApplicationPermissions.Assignments.Default,
            policy => policy.RequireClaim(PermissionConstant, GrantApplicationPermissions.Assignments.Default));
        authorizationBuilder.AddPolicy(GrantApplicationPermissions.Assignments.AssignInitial,
            policy => policy.RequireClaim(PermissionConstant, GrantApplicationPermissions.Assignments.AssignInitial));
        authorizationBuilder.AddPolicy(GrantApplicationPermissions.Reviews.Default,
            policy => policy.RequireClaim(PermissionConstant, GrantApplicationPermissions.Reviews.Default));
        authorizationBuilder.AddPolicy(GrantApplicationPermissions.Reviews.StartInitial,
            policy => policy.RequireClaim(PermissionConstant, GrantApplicationPermissions.Reviews.StartInitial));
        authorizationBuilder.AddPolicy(GrantApplicationPermissions.Reviews.CompleteInitial,
            policy => policy.RequireClaim(PermissionConstant, GrantApplicationPermissions.Reviews.CompleteInitial));
        authorizationBuilder.AddPolicy(GrantApplicationPermissions.Approvals.Default,
            policy => policy.RequireClaim(PermissionConstant, GrantApplicationPermissions.Approvals.Default));
        authorizationBuilder.AddPolicy(GrantApplicationPermissions.Approvals.Complete,
            policy => policy.RequireClaim(PermissionConstant, GrantApplicationPermissions.Approvals.Complete));
        authorizationBuilder.AddPolicy(GrantApplicationPermissions.Comments.Default,
            policy => policy.RequireClaim(PermissionConstant, GrantApplicationPermissions.Comments.Default));
        authorizationBuilder.AddPolicy(GrantApplicationPermissions.Comments.Add,
            policy => policy.RequireClaim(PermissionConstant, GrantApplicationPermissions.Comments.Add));

        // R&A Policies
        authorizationBuilder.AddPolicy(UnitySelector.Review.Default,
            policy => policy.RequireClaim(PermissionConstant, UnitySelector.Review.Default));

        // R&A - Approval Policies
        authorizationBuilder.AddPolicy(UnitySelector.Review.Approval.Default,
            policy => policy.RequireClaim(PermissionConstant, UnitySelector.Review.Approval.Default));
        authorizationBuilder.AddPolicy(UnitySelector.Review.Approval.Update.Default,
            policy => policy.RequireClaim(PermissionConstant, UnitySelector.Review.Approval.Update.Default));
        authorizationBuilder.AddPolicy(UnitySelector.Review.Approval.Update.UpdateFinalStateFields,
            policy => policy.RequireClaim(PermissionConstant, UnitySelector.Review.Approval.Update.UpdateFinalStateFields));

        // R&A - Assessment Results Policies
        authorizationBuilder.AddPolicy(UnitySelector.Review.AssessmentResults.Default,
            policy => policy.RequireClaim(PermissionConstant, UnitySelector.Review.AssessmentResults.Default));
        authorizationBuilder.AddPolicy(UnitySelector.Review.AssessmentResults.Update.Default,
            policy => policy.RequireClaim(PermissionConstant, UnitySelector.Review.AssessmentResults.Update.Default));
        authorizationBuilder.AddPolicy(UnitySelector.Review.AssessmentResults.Update.UpdateFinalStateFields,
            policy => policy.RequireClaim(PermissionConstant, UnitySelector.Review.AssessmentResults.Update.UpdateFinalStateFields));

        // R&A - Assessment Review List Policies
        authorizationBuilder.AddPolicy(UnitySelector.Review.AssessmentReviewList.Default,
            policy => policy.RequireClaim(PermissionConstant, UnitySelector.Review.AssessmentReviewList.Default));
        authorizationBuilder.AddPolicy(UnitySelector.Review.AssessmentReviewList.Create,
            policy => policy.RequireClaim(PermissionConstant, UnitySelector.Review.AssessmentReviewList.Create));
        authorizationBuilder.AddPolicy(UnitySelector.Review.AssessmentReviewList.Update.SendBack,
            policy => policy.RequireClaim(PermissionConstant, UnitySelector.Review.AssessmentReviewList.Update.SendBack));
        authorizationBuilder.AddPolicy(UnitySelector.Review.AssessmentReviewList.Update.Complete,
            policy => policy.RequireClaim(PermissionConstant, UnitySelector.Review.AssessmentReviewList.Update.Complete));

        // Tenancy Policies
        authorizationBuilder.AddPolicy(TenantManagementPermissions.Tenants.Default,
            policy => policy.RequireClaim(PermissionConstant, TenantManagementPermissions.Tenants.Default));
        authorizationBuilder.AddPolicy(TenantManagementPermissions.Tenants.Create,
            policy => policy.RequireClaim(PermissionConstant, TenantManagementPermissions.Tenants.Create));
        authorizationBuilder.AddPolicy(TenantManagementPermissions.Tenants.Update,
            policy => policy.RequireClaim(PermissionConstant, TenantManagementPermissions.Tenants.Update));
        authorizationBuilder.AddPolicy(TenantManagementPermissions.Tenants.Delete,
            policy => policy.RequireClaim(PermissionConstant, TenantManagementPermissions.Tenants.Delete));
        authorizationBuilder.AddPolicy(TenantManagementPermissions.Tenants.ManageFeatures,
            policy => policy.RequireClaim(PermissionConstant, TenantManagementPermissions.Tenants.ManageFeatures));
        authorizationBuilder.AddPolicy(TenantManagementPermissions.Tenants.ManageConnectionStrings,
            policy => policy.RequireClaim(PermissionConstant, TenantManagementPermissions.Tenants.ManageConnectionStrings));

        // Setting Management - Tag Management
        authorizationBuilder.AddPolicy(UnitySelector.SettingManagement.Tags.Default,
            policy => policy.RequireClaim(PermissionConstant, UnitySelector.SettingManagement.Tags.Default));
        authorizationBuilder.AddPolicy(UnitySelector.SettingManagement.Tags.Update,
            policy => policy.RequireClaim(PermissionConstant, UnitySelector.SettingManagement.Tags.Update));
        authorizationBuilder.AddPolicy(UnitySelector.SettingManagement.Tags.Delete,
            policy => policy.RequireClaim(PermissionConstant, UnitySelector.SettingManagement.Tags.Delete));

        // IT Administrator Policies
        authorizationBuilder.AddPolicy(IdentityConsts.ITAdminPolicyName,
        policy => policy.RequireAssertion(context =>
            context.User.IsInRole(IdentityConsts.ITAdminRoleName) ||
            context.User.HasClaim(c => c.Type == PermissionConstant && c.Value == IdentityConsts.ITAdminPermissionName)
        ));

        // Project Info Policies
        authorizationBuilder.AddPolicy(UnitySelector.Project.Default,
            policy => policy.RequireClaim(PermissionConstant, UnitySelector.Project.Default));

        // Project Info Logical OR policy
        authorizationBuilder.AddPolicy(UnitySelector.Project.UpdatePolicy,
            policy => policy.RequireAssertion(context => 
            context.User.HasClaim(PermissionConstant, UnitySelector.Project.Location.Update.Default) ||
            context.User.HasClaim(PermissionConstant, UnitySelector.Project.Summary.Update.Default) ||
            
            // NOTE: This will be replaced when Worksheets are normalized with UnitySelector.Project.Worksheet.Update
            context.User.HasClaim(PermissionConstant, UnitySelector.Project.Default) 
        ));

        // Project Info - Summary Policies
        authorizationBuilder.AddPolicy(UnitySelector.Project.Summary.Default,
            policy => policy.RequireClaim(PermissionConstant, UnitySelector.Project.Summary.Default));
        authorizationBuilder.AddPolicy(UnitySelector.Project.Summary.Update.Default,
            policy => policy.RequireClaim(PermissionConstant, UnitySelector.Project.Summary.Update.Default));
        authorizationBuilder.AddPolicy(UnitySelector.Project.Summary.Update.UpdateFinalStateFields,
            policy => policy.RequireClaim(PermissionConstant, UnitySelector.Project.Summary.Update.UpdateFinalStateFields));

        // Project Info - Location Policies
        authorizationBuilder.AddPolicy(UnitySelector.Project.Location.Default,
            policy => policy.RequireClaim(PermissionConstant, UnitySelector.Project.Location.Default));
        authorizationBuilder.AddPolicy(UnitySelector.Project.Location.Update.Default,
            policy => policy.RequireClaim(PermissionConstant, UnitySelector.Project.Location.Update.Default));
        authorizationBuilder.AddPolicy(UnitySelector.Project.Location.Update.UpdateFinalStateFields,
            policy => policy.RequireClaim(PermissionConstant, UnitySelector.Project.Location.Update.UpdateFinalStateFields));

        // Project Info - Worksheet Policies
        authorizationBuilder.AddPolicy(UnitySelector.Project.Worksheet.Default,
            policy => policy.RequireClaim(PermissionConstant, UnitySelector.Project.Worksheet.Default));  // NOTE: Will be replaced when Worksheets normalized
        
        authorizationBuilder.AddPolicy(UnitySelector.Project.Worksheet.Update,
            policy => policy.RequireClaim(PermissionConstant, UnitySelector.Project.Worksheet.Update));  // NOTE: Will be replaced when Worksheets normalized
    }
}

