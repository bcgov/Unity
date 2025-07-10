using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Identity;
using Unity.Modules.Shared;
using Unity.Notifications.Permissions;
using Unity.Payments.Permissions;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.PermissionManagement;

namespace Unity.GrantManager.Permissions
{
    internal class PermissionGrantsDataSeeder : IDataSeedContributor, ITransientDependency
    {
        private readonly IPermissionDataSeeder _permissionDataSeeder;

        public PermissionGrantsDataSeeder(IPermissionDataSeeder permissionDataSeeder)
        {
            _permissionDataSeeder = permissionDataSeeder;
        }

        public readonly List<string> ReviewAndAssessment_CommonPermissions = [
            UnitySelector.Review.Default,
            UnitySelector.Review.Approval.Default,
            UnitySelector.Review.Approval.Update.Default,

            UnitySelector.Review.AssessmentResults.Default,
            UnitySelector.Review.AssessmentResults.Update.Default,

            UnitySelector.Review.AssessmentReviewList.Default,
            UnitySelector.Review.AssessmentReviewList.Create,
            UnitySelector.Review.AssessmentReviewList.Update.SendBack,
            UnitySelector.Review.AssessmentReviewList.Update.Complete
        ];

        public readonly List<string> ApplicantInfo_CommonPermissions = [
            UnitySelector.Applicant.Default,
            UnitySelector.Applicant.Summary.Default,
            UnitySelector.Applicant.Summary.Update,
            UnitySelector.Applicant.Contact.Default,
            UnitySelector.Applicant.Contact.Update,
            UnitySelector.Applicant.Authority.Default,
            UnitySelector.Applicant.Authority.Update,
            UnitySelector.Applicant.Location.Default,
            UnitySelector.Applicant.Location.Update,
            UnitySelector.Applicant.AdditionalContact.Default,
            UnitySelector.Applicant.AdditionalContact.Create,
            UnitySelector.Applicant.AdditionalContact.Update,

        ];

        public readonly List<string> ProjectInfo_CommonPermissions = [
            UnitySelector.Project.Default,
            UnitySelector.Project.Summary.Default,
            UnitySelector.Project.Summary.Update.Default,
            UnitySelector.Project.Location.Default,
            UnitySelector.Project.Location.Update.Default,
        ];

        public readonly List<string> PaymentInfo_CommonPermissions = [
            UnitySelector.Payment.Summary.Default,
            UnitySelector.Payment.Supplier.Default,
            UnitySelector.Payment.Supplier.Update,
            UnitySelector.Payment.PaymentList.Default
        ];

        public readonly List<string> Notifications_CommonPermissions = [
            NotificationsPermissions.Email.Default,
            NotificationsPermissions.Email.Send,
        ];

        public readonly List<string> Dashboard_CommonPermissions = [
            GrantApplicationPermissions.Dashboard.Default,
            GrantApplicationPermissions.Dashboard.ViewDashboard,
            GrantApplicationPermissions.Dashboard.ApplicationStatusCount,
            GrantApplicationPermissions.Dashboard.EconomicRegionCount,
            GrantApplicationPermissions.Dashboard.ApplicationTagsCount,
            GrantApplicationPermissions.Dashboard.ApplicationAssigneeCount,
            GrantApplicationPermissions.Dashboard.RequestedAmountPerSubsector,
            GrantApplicationPermissions.Dashboard.RequestApprovedCount,
        ];

        public readonly List<string> SettingManagement_Tags_CommonPermissions = [
            UnitySelector.SettingManagement.Tags.Default,
            UnitySelector.SettingManagement.Tags.Create,
            UnitySelector.SettingManagement.Tags.Update,
            UnitySelector.SettingManagement.Tags.Delete
        ];

        public async Task SeedAsync(DataSeedContext context)
        {
            // Default permission grants based on role

            // - Program Manager
            await _permissionDataSeeder.SeedAsync(RolePermissionValueProvider.ProviderName, UnityRoles.ProgramManager,
            [
                    GrantManagerPermissions.Default,
                    GrantApplicationPermissions.Applications.Default,
                    GrantApplicationPermissions.Assignments.AssignInitial,
                    GrantApplicationPermissions.Reviews.StartInitial,
                    GrantApplicationPermissions.Reviews.CompleteInitial,
                    GrantApplicationPermissions.Comments.Add,
                    GrantManagerPermissions.Organizations.Default,
                    GrantManagerPermissions.Organizations.ManageProfiles,
                    IdentitySeedPermissions.Users.Default,
                    IdentitySeedPermissions.Users.Create,
                    IdentitySeedPermissions.Users.Update,
                    IdentitySeedPermissions.Users.Delete,
                    IdentitySeedPermissions.Users.ManagePermissions,
                    IdentitySeedPermissions.Roles.Default,
                    IdentitySeedPermissions.Roles.Create,
                    IdentitySeedPermissions.Roles.Update,
                    IdentitySeedPermissions.Roles.Delete,
                    IdentitySeedPermissions.Roles.ManagePermissions,
                    GrantManagerPermissions.Intakes.Default,
                    GrantManagerPermissions.ApplicationForms.Default,

                    .. SettingManagement_Tags_CommonPermissions,
                    .. ReviewAndAssessment_CommonPermissions,
                    .. ApplicantInfo_CommonPermissions,
                    .. ProjectInfo_CommonPermissions,
                    .. Notifications_CommonPermissions,
                    .. Dashboard_CommonPermissions
            ], context.TenantId);

            // - Reviewer
            await _permissionDataSeeder.SeedAsync(RolePermissionValueProvider.ProviderName, UnityRoles.Reviewer,
                [
                    GrantManagerPermissions.Default,
                    GrantApplicationPermissions.Applications.Default,
                    GrantApplicationPermissions.Reviews.StartInitial,
                    GrantApplicationPermissions.Reviews.CompleteInitial,
                    GrantApplicationPermissions.Comments.Add,

                    .. ReviewAndAssessment_CommonPermissions,
                    .. ApplicantInfo_CommonPermissions,
                    .. ProjectInfo_CommonPermissions,
                    .. Notifications_CommonPermissions,
                    .. Dashboard_CommonPermissions
                ], context.TenantId);

            // - Assessor
            await _permissionDataSeeder.SeedAsync(RolePermissionValueProvider.ProviderName, UnityRoles.Assessor,
                [
                    GrantManagerPermissions.Default,
                    GrantApplicationPermissions.Applications.Default,
                    GrantApplicationPermissions.Reviews.StartInitial,
                    GrantApplicationPermissions.Reviews.CompleteInitial,
                    GrantApplicationPermissions.Comments.Add,

                    .. ReviewAndAssessment_CommonPermissions,
                    .. ApplicantInfo_CommonPermissions,
                    .. ProjectInfo_CommonPermissions,
                    .. Notifications_CommonPermissions,
                    .. Dashboard_CommonPermissions
                ], context.TenantId);

            // - TeamLead
            await _permissionDataSeeder.SeedAsync(RolePermissionValueProvider.ProviderName, UnityRoles.TeamLead,
                [
                    GrantManagerPermissions.Default,
                    GrantApplicationPermissions.Applications.Default,
                    GrantApplicationPermissions.Assignments.AssignInitial,
                    GrantApplicationPermissions.Applicants.AssignApplicant,
                    GrantApplicationPermissions.Reviews.StartInitial,
                    GrantApplicationPermissions.Reviews.CompleteInitial,
                    GrantApplicationPermissions.Comments.Add,
                    GrantManagerPermissions.Organizations.Default,
                    GrantManagerPermissions.Organizations.ManageProfiles,
                    GrantApplicationPermissions.Approvals.BulkApplicationApproval,
                    GrantApplicationPermissions.Approvals.DeferAfterApproval,

                    .. SettingManagement_Tags_CommonPermissions,
                    .. ReviewAndAssessment_CommonPermissions,
                    .. ApplicantInfo_CommonPermissions,
                    .. ProjectInfo_CommonPermissions,
                    .. Notifications_CommonPermissions,
                    .. Dashboard_CommonPermissions,

                    // Role Specific Permissions
                    UnitySelector.Project.Summary.Update.UpdateFinalStateFields,
                    UnitySelector.Project.Location.Update.UpdateFinalStateFields,
                ], context.TenantId);

            // - Approver
            await _permissionDataSeeder.SeedAsync(RolePermissionValueProvider.ProviderName, UnityRoles.Approver,
                [
                    GrantManagerPermissions.Default,
                    GrantApplicationPermissions.Applications.Default,
                    GrantApplicationPermissions.Approvals.Complete,
                    GrantApplicationPermissions.Approvals.DeferAfterApproval,
                    GrantApplicationPermissions.Comments.Add,

                    .. ReviewAndAssessment_CommonPermissions,
                    .. ApplicantInfo_CommonPermissions,
                    .. ProjectInfo_CommonPermissions,
                    .. Notifications_CommonPermissions,
                    .. Dashboard_CommonPermissions
                ], context.TenantId);

            // - SystemAdmin
            await _permissionDataSeeder.SeedAsync(RolePermissionValueProvider.ProviderName, UnityRoles.SystemAdmin,
                [
                    GrantManagerPermissions.Default,
                    UnitySettingManagementPermissions.UserInterface,
                    GrantManagerPermissions.Organizations.Default,
                    GrantManagerPermissions.Organizations.ManageProfiles,
                    GrantManagerPermissions.Intakes.Default,
                    GrantManagerPermissions.ApplicationForms.Default,


                    .. SettingManagement_Tags_CommonPermissions,
                    .. ReviewAndAssessment_CommonPermissions,
                    .. ApplicantInfo_CommonPermissions,
                    .. ProjectInfo_CommonPermissions,
                    .. Notifications_CommonPermissions,
                    NotificationsPermissions.Settings,
                    .. Dashboard_CommonPermissions,

                    UnitySettingManagementPermissions.BackgroundJobSettings,
                ], context.TenantId);


            // -L1 Approver
            await _permissionDataSeeder.SeedAsync(RolePermissionValueProvider.ProviderName, UnityRoles.L1Approver,
                [
                    GrantManagerPermissions.Default,
                    GrantApplicationPermissions.Applications.Default,
                    PaymentsPermissions.Payments.Default,
                    PaymentsPermissions.Payments.L1ApproveOrDecline,

                    .. ReviewAndAssessment_CommonPermissions,
                    .. ApplicantInfo_CommonPermissions,
                    .. ProjectInfo_CommonPermissions,
                    .. Notifications_CommonPermissions,
                    .. Dashboard_CommonPermissions
                ], context.TenantId);

            // -L2 Approver
            await _permissionDataSeeder.SeedAsync(RolePermissionValueProvider.ProviderName, UnityRoles.L2Approver,
                [
                    GrantManagerPermissions.Default,
                    GrantApplicationPermissions.Applications.Default,
                    PaymentsPermissions.Payments.Default,
                    PaymentsPermissions.Payments.L2ApproveOrDecline,

                    .. ReviewAndAssessment_CommonPermissions,
                    .. ApplicantInfo_CommonPermissions,
                    .. ProjectInfo_CommonPermissions,
                    .. Notifications_CommonPermissions,
                    .. Dashboard_CommonPermissions
                ], context.TenantId);

            // -L3 Approver
            await _permissionDataSeeder.SeedAsync(RolePermissionValueProvider.ProviderName, UnityRoles.L3Approver,
                [
                    GrantManagerPermissions.Default,
                    GrantApplicationPermissions.Applications.Default,
                    PaymentsPermissions.Payments.Default,
                    PaymentsPermissions.Payments.L3ApproveOrDecline,

                    .. ReviewAndAssessment_CommonPermissions,
                    .. ApplicantInfo_CommonPermissions,
                    .. ProjectInfo_CommonPermissions,
                    .. Notifications_CommonPermissions,
                    .. Dashboard_CommonPermissions
                ], context.TenantId);

            // -External Assessor
            await _permissionDataSeeder.SeedAsync(RolePermissionValueProvider.ProviderName, UnityRoles.ExternalAssessor,
                [
                    GrantManagerPermissions.Default,
                    GrantApplicationPermissions.Applications.Default,
                    PaymentsPermissions.Payments.Default,

                    UnitySelector.Review.Default,
                    UnitySelector.Review.Approval.Default,
                    UnitySelector.Review.AssessmentResults.Default,
                    UnitySelector.Review.AssessmentReviewList.Default,
                    UnitySelector.Review.AssessmentReviewList.Create,
                    UnitySelector.Review.AssessmentReviewList.Update.SendBack,
                    UnitySelector.Review.AssessmentReviewList.Update.Complete,
                    UnitySelector.Review.Worksheet.Default,

                    UnitySelector.Applicant.Default,
                    UnitySelector.Applicant.Summary.Default,
                    UnitySelector.Applicant.Contact.Default,
                    UnitySelector.Applicant.Authority.Default,
                    UnitySelector.Applicant.Location.Default,
                    UnitySelector.Applicant.AdditionalContact.Default,

                    UnitySelector.Project.Default,
                    UnitySelector.Project.Summary.Default,
                    UnitySelector.Project.Location.Default,

                    NotificationsPermissions.Email.Default,
                ], context.TenantId);

        }
    }
}

