using Microsoft.Extensions.DependencyInjection;
using Unity.GrantManager.Permissions;
using Unity.Modules.Shared;
using Unity.TenantManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;

namespace Unity.GrantManager.Web.Identity.Policy;

internal static class PolicyRegistrant
{
    internal static void Register(ServiceConfigurationContext context)
    {
        // ABP should do this for us when a permission definition is added, but does not seem to work with our setup

        context.Services.AddAuthorization(options =>
            options.AddPolicy(IdentityPermissions.Roles.Default,
            policy => policy.RequireClaim("Permission", IdentityPermissions.Roles.Default)));
        context.Services.AddAuthorization(options =>
            options.AddPolicy(IdentityPermissions.Roles.Create,
            policy => policy.RequireClaim("Permission", IdentityPermissions.Roles.Create)));
        context.Services.AddAuthorization(options =>
           options.AddPolicy(IdentityPermissions.Roles.Update,
           policy => policy.RequireClaim("Permission", IdentityPermissions.Roles.Update)));
        context.Services.AddAuthorization(options =>
           options.AddPolicy(IdentityPermissions.Roles.Delete,
           policy => policy.RequireClaim("Permission", IdentityPermissions.Roles.Delete)));
        context.Services.AddAuthorization(options =>
           options.AddPolicy(IdentityPermissions.Roles.ManagePermissions,
           policy => policy.RequireClaim("Permission", IdentityPermissions.Roles.ManagePermissions)));

        context.Services.AddAuthorization(options =>
           options.AddPolicy(IdentityPermissions.Users.Default,
           policy => policy.RequireClaim("Permission", IdentityPermissions.Users.Default)));
        context.Services.AddAuthorization(options =>
           options.AddPolicy(IdentityPermissions.Users.Create,
           policy => policy.RequireClaim("Permission", IdentityPermissions.Users.Create)));
        context.Services.AddAuthorization(options =>
           options.AddPolicy(IdentityPermissions.Users.Update,
           policy => policy.RequireClaim("Permission", IdentityPermissions.Users.Update)));
        context.Services.AddAuthorization(options =>
           options.AddPolicy(IdentityPermissions.Users.Delete,
           policy => policy.RequireClaim("Permission", IdentityPermissions.Users.Delete)));
        context.Services.AddAuthorization(options =>
           options.AddPolicy(IdentityPermissions.Users.ManagePermissions,
           policy => policy.RequireClaim("Permission", IdentityPermissions.Users.ManagePermissions)));

        context.Services.AddAuthorization(options =>
           options.AddPolicy(IdentityPermissions.UserLookup.Default,
           policy => policy.RequireClaim("Permission", IdentityPermissions.UserLookup.Default)));

        context.Services.AddAuthorization(options =>
            options.AddPolicy(GrantManagerPermissions.Default,
            policy => policy.RequireClaim("Permission", GrantManagerPermissions.Default)));

        context.Services.AddAuthorization(options =>
            options.AddPolicy(GrantApplicationPermissions.Applications.Default,
            policy => policy.RequireClaim("Permission", GrantApplicationPermissions.Applications.Default)));

        context.Services.AddAuthorization(options =>
           options.AddPolicy(GrantApplicationPermissions.Applicants.Default,
           policy => policy.RequireClaim("Permission", GrantApplicationPermissions.Applicants.Default)));
        context.Services.AddAuthorization(options =>
           options.AddPolicy(GrantApplicationPermissions.Applicants.Edit,
           policy => policy.RequireClaim("Permission", GrantApplicationPermissions.Applicants.Edit)));

        context.Services.AddAuthorization(options =>
           options.AddPolicy(GrantApplicationPermissions.Assignments.Default,
           policy => policy.RequireClaim("Permission", GrantApplicationPermissions.Assignments.Default)));
        context.Services.AddAuthorization(options =>
           options.AddPolicy(GrantApplicationPermissions.Assignments.AssignInitial,
           policy => policy.RequireClaim("Permission", GrantApplicationPermissions.Assignments.AssignInitial)));

        context.Services.AddAuthorization(options =>
           options.AddPolicy(GrantApplicationPermissions.Reviews.Default,
           policy => policy.RequireClaim("Permission", GrantApplicationPermissions.Reviews.Default)));
        context.Services.AddAuthorization(options =>
           options.AddPolicy(GrantApplicationPermissions.Reviews.StartInitial,
           policy => policy.RequireClaim("Permission", GrantApplicationPermissions.Reviews.StartInitial)));
        context.Services.AddAuthorization(options =>
           options.AddPolicy(GrantApplicationPermissions.Reviews.CompleteInitial,
           policy => policy.RequireClaim("Permission", GrantApplicationPermissions.Reviews.CompleteInitial)));

        context.Services.AddAuthorization(options =>
           options.AddPolicy(GrantApplicationPermissions.Approvals.Default,
           policy => policy.RequireClaim("Permission", GrantApplicationPermissions.Approvals.Default)));
        context.Services.AddAuthorization(options =>
           options.AddPolicy(GrantApplicationPermissions.Approvals.Complete,
           policy => policy.RequireClaim("Permission", GrantApplicationPermissions.Approvals.Complete)));

        context.Services.AddAuthorization(options =>
           options.AddPolicy(GrantApplicationPermissions.Comments.Default,
           policy => policy.RequireClaim("Permission", GrantApplicationPermissions.Comments.Default)));
        context.Services.AddAuthorization(options =>
           options.AddPolicy(GrantApplicationPermissions.Comments.Add,
           policy => policy.RequireClaim("Permission", GrantApplicationPermissions.Comments.Add)));

        context.Services.AddAuthorization(options =>
            options.AddPolicy(GrantManagerPermissions.Intakes.Default,
            policy => policy.RequireClaim("Permission", GrantManagerPermissions.Intakes.Default)));
        context.Services.AddAuthorization(options =>
            options.AddPolicy(GrantManagerPermissions.ApplicationForms.Default,
            policy => policy.RequireClaim("Permission", GrantManagerPermissions.ApplicationForms.Default)));

        //-- R&A - Approval
        context.Services.AddAuthorization(options =>
            options.AddPolicy(UnitySelector.Review.AssessmentResults.Default,
            policy => policy.RequireClaim("Permission", UnitySelector.Review.Approval.Default)));
        context.Services.AddAuthorization(options =>
            options.AddPolicy(UnitySelector.Review.AssessmentResults.Update.Default,
            policy => policy.RequireClaim("Permission", UnitySelector.Review.Approval.Update.Default)));
        //context.Services.AddAuthorization(options =>
        //    options.AddPolicy(UnitySelector.Review.AssessmentResults.Update.UpdateFinalStateFields,
        //    policy => policy.RequireClaim("Permission", UnitySelector.Review.Approval.Update.UpdateFinalStateFields)));

        //-- R&A - Assessment Results
        context.Services.AddAuthorization(options =>
            options.AddPolicy(UnitySelector.Review.AssessmentResults.Default,
            policy => policy.RequireClaim("Permission", UnitySelector.Review.AssessmentResults.Default)));
        context.Services.AddAuthorization(options =>
            options.AddPolicy(UnitySelector.Review.AssessmentResults.Update.Default,
            policy => policy.RequireClaim("Permission", UnitySelector.Review.AssessmentResults.Update.Default)));
        context.Services.AddAuthorization(options =>
            options.AddPolicy(UnitySelector.Review.AssessmentResults.Update.UpdateFinalStateFields,
            policy => policy.RequireClaim("Permission", UnitySelector.Review.AssessmentResults.Update.UpdateFinalStateFields)));

        //-- R&A - Assessment Review List and List Items
        context.Services.AddAuthorization(options =>
            options.AddPolicy(UnitySelector.Review.AssessmentReviewList.Default,
            policy => policy.RequireClaim("Permission", UnitySelector.Review.AssessmentReviewList.Default)));
        context.Services.AddAuthorization(options =>
            options.AddPolicy(UnitySelector.Review.AssessmentReviewList.Create,
            policy => policy.RequireClaim("Permission", UnitySelector.Review.AssessmentReviewList.Create)));
        context.Services.AddAuthorization(options =>
            options.AddPolicy(UnitySelector.Review.AssessmentReviewList.Update.SendBack,
            policy => policy.RequireClaim("Permission", UnitySelector.Review.AssessmentReviewList.Update.SendBack)));
        context.Services.AddAuthorization(options =>
            options.AddPolicy(UnitySelector.Review.AssessmentReviewList.Update.Complete,
            policy => policy.RequireClaim("Permission", UnitySelector.Review.AssessmentReviewList.Update.Complete)));

        // Tenancy
        context.Services.AddAuthorization(options =>
            options.AddPolicy(TenantManagementPermissions.Tenants.Default,
            policy => policy.RequireClaim("Permission", TenantManagementPermissions.Tenants.Default)));
        context.Services.AddAuthorization(options =>
            options.AddPolicy(TenantManagementPermissions.Tenants.Create,
            policy => policy.RequireClaim("Permission", TenantManagementPermissions.Tenants.Create)));
        context.Services.AddAuthorization(options =>
            options.AddPolicy(TenantManagementPermissions.Tenants.Update,
            policy => policy.RequireClaim("Permission", TenantManagementPermissions.Tenants.Update)));
        context.Services.AddAuthorization(options =>
            options.AddPolicy(TenantManagementPermissions.Tenants.Delete,
            policy => policy.RequireClaim("Permission", TenantManagementPermissions.Tenants.Delete)));
        context.Services.AddAuthorization(options =>
            options.AddPolicy(TenantManagementPermissions.Tenants.ManageFeatures,
            policy => policy.RequireClaim("Permission", TenantManagementPermissions.Tenants.ManageFeatures)));
        context.Services.AddAuthorization(options =>
           options.AddPolicy(TenantManagementPermissions.Tenants.ManageConnectionStrings,
           policy => policy.RequireClaim("Permission", TenantManagementPermissions.Tenants.ManageConnectionStrings)));
    }
}

