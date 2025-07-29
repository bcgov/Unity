using Unity.GrantManager.Localization;
using Unity.Modules.Shared;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Localization;
using Volo.Abp.SettingManagement;

namespace Unity.GrantManager.Permissions.GrantApplications
{
    public class GrantApplicationPermissionDefinitionProvider : PermissionDefinitionProvider
    {
        public override void Define(IPermissionDefinitionContext context)
        {
            var grantApplicationPermissionsGroup = context.AddGroup(GrantApplicationPermissions.GroupName, L("Permission:GrantApplicationManagement"));

            // Dashboard
            var dashboardPermissions = grantApplicationPermissionsGroup.AddPermission(
                GrantApplicationPermissions.Dashboard.Default,
                L("Permission:GrantApplicationManagement.Dashboard.Default"));

            var viewDashboard = dashboardPermissions.AddChild(
                GrantApplicationPermissions.Dashboard.ViewDashboard,
                L("Permission:GrantApplicationManagement.Dashboard.ViewDashboard"));
            viewDashboard.AddChild(
                GrantApplicationPermissions.Dashboard.ApplicationStatusCount,
                L("Permission:GrantApplicationManagement.Dashboard.ApplicationStatusCount"));
            viewDashboard.AddChild(
                GrantApplicationPermissions.Dashboard.EconomicRegionCount,
                L("Permission:GrantApplicationManagement.Dashboard.EconomicRegionCount"));
            viewDashboard.AddChild(
                GrantApplicationPermissions.Dashboard.ApplicationTagsCount,
                L("Permission:GrantApplicationManagement.Dashboard.ApplicationTagsCount"));
            viewDashboard.AddChild(
                GrantApplicationPermissions.Dashboard.ApplicationAssigneeCount,
                L("Permission:GrantApplicationManagement.Dashboard.ApplicationAssigneeCount"));
            viewDashboard.AddChild(
                GrantApplicationPermissions.Dashboard.RequestedAmountPerSubsector,
                L("Permission:GrantApplicationManagement.Dashboard.RequestedAmountPerSubsector"));
            viewDashboard.AddChild(
                GrantApplicationPermissions.Dashboard.RequestApprovedCount,
                L("Permission:GrantApplicationManagement.Dashboard.RequestApprovedCount"));

            // Application
            grantApplicationPermissionsGroup.AddPermission(GrantApplicationPermissions.Applications.Default, L("Permission:GrantApplicationManagement.Applications.Default"));

            // Applicant
            var applicatPermissions = grantApplicationPermissionsGroup.AddPermission(GrantApplicationPermissions.Applicants.Default, L("Permission:GrantApplicationManagement.Applicants.Default"));
            applicatPermissions.AddChild(GrantApplicationPermissions.Applicants.Edit, L("Permission:GrantApplicationManagement.Applicants.Edit"));
            applicatPermissions.AddChild(GrantApplicationPermissions.Applicants.AssignApplicant, L("Permission:GrantApplicationManagement.Applicants.AssignApplicant"));

            // Assignment
            var assignmentPermissions = grantApplicationPermissionsGroup.AddPermission(GrantApplicationPermissions.Assignments.Default, L("Permission:GrantApplicationManagement.Assignments.Default"));
            assignmentPermissions.AddChild(GrantApplicationPermissions.Assignments.AssignInitial, L("Permission:GrantApplicationManagement.Assignments.AssignInitial"));

            // Review
            var reviewPermissions = grantApplicationPermissionsGroup.AddPermission(GrantApplicationPermissions.Reviews.Default, L("Permission:GrantApplicationManagement.Reviews.Default"));
            reviewPermissions.AddChild(GrantApplicationPermissions.Reviews.StartInitial, L("Permission:GrantApplicationManagement.Reviews.StartInitial"));
            reviewPermissions.AddChild(GrantApplicationPermissions.Reviews.CompleteInitial, L("Permission:GrantApplicationManagement.Reviews.CompleteInitial"));

            // Approval
            var approvalPermissions = grantApplicationPermissionsGroup.AddPermission(GrantApplicationPermissions.Approvals.Default, L("Permission:GrantApplicationManagement.Approvals.Default"));
            approvalPermissions.AddChild(GrantApplicationPermissions.Approvals.Complete, L("Permission:GrantApplicationManagement.Approvals.Complete"));
            approvalPermissions.AddChild(GrantApplicationPermissions.Approvals.DeferAfterApproval, L("Permission:GrantApplicationManagement.Approvals.DeferAfterApproval"));
            approvalPermissions.AddChild(GrantApplicationPermissions.Approvals.BulkApplicationApproval, L("Permission:GrantApplicationManagement.Approvals.BulkApplicationApproval"));

            // Comments
            var appCommentPermissions = grantApplicationPermissionsGroup.AddPermission(GrantApplicationPermissions.Comments.Default, L("Permission:GrantApplicationManagement.Comments.Default"));
            appCommentPermissions.AddChild(GrantApplicationPermissions.Comments.Add, L("Permission:GrantApplicationManagement.Comments.Add"));

            //-- REVIEW & ASSESSMENT PERMISSIONS
            grantApplicationPermissionsGroup.AddApplication_ReviewAndAssessment_Permissions();

            //-- APPLICANT INFO PERMISSIONS
            grantApplicationPermissionsGroup.AddApplication_ApplicantInfo_Permissions();

            //-- PROJECT INFO PERMISSIONS
            grantApplicationPermissionsGroup.AddApplication_ProjectInfo_Permissions();

            var settingManagement = context.GetGroup(SettingManagementPermissions.GroupName);
            settingManagement.AddPermission(UnitySettingManagementPermissions.UserInterface, L("Permission:UnitySettingManagementPermissions.UserInterface"));
            settingManagement.AddPermission(UnitySettingManagementPermissions.BackgroundJobSettings, L("Permission:UnitySettingManagementPermissions.BackgroundJobs"));
            settingManagement.AddPermission(UnitySettingManagementPermissions.ConfigurePayments, L("Permission:UnitySettingManagementPermissions.ConfigurePayments"));

            // Settings - Tag Management
            var tagManagement = settingManagement.AddPermission(UnitySelector.SettingManagement.Tags.Default, L(UnitySelector.SettingManagement.Tags.Default));
            tagManagement.AddChild(UnitySelector.SettingManagement.Tags.Create, L(UnitySelector.SettingManagement.Tags.Create));
            tagManagement.AddChild(UnitySelector.SettingManagement.Tags.Update, L(UnitySelector.SettingManagement.Tags.Update));
            tagManagement.AddChild(UnitySelector.SettingManagement.Tags.Delete, L(UnitySelector.SettingManagement.Tags.Delete));

            var emailingPermission = context.GetPermissionOrNull(SettingManagementPermissions.Emailing);
            if (emailingPermission != null)
            {
                emailingPermission.IsEnabled = false;
            }

            var emailingTestPermission = context.GetPermissionOrNull(SettingManagementPermissions.EmailingTest);
            if (emailingTestPermission != null)
            {
                emailingTestPermission.IsEnabled = false;
            }

            var timezonePermission = context.GetPermissionOrNull(SettingManagementPermissions.TimeZone);
            if (timezonePermission != null)
            {
                timezonePermission.IsEnabled = false;
            }
        }

        private static LocalizableString L(string name)
        {
            return LocalizableString.Create<GrantManagerResource>(name);
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S125:Sections of code should not be commented out", Justification = "Configuration Code")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S1481:Unused local variables should be removed", Justification = "Configuration Code")]
    public static class PermissionGroupDefinitionExtensions
    {
        public static void AddApplication_ReviewAndAssessment_Permissions(this PermissionGroupDefinition grantApplicationPermissionsGroup)
        {
            #region REVIEW & ASSESSMENT GRANULAR PERMISSIONS
            var upx_Review                                          = grantApplicationPermissionsGroup.AddPermission(UnitySelector.Review.Default, L(UnitySelector.Review.Default));

            var upx_Review_Approval                                 = upx_Review.AddUnityChild(UnitySelector.Review.Approval.Default);
            var upx_Review_Approval_Update                          = upx_Review_Approval.AddUnityChild(UnitySelector.Review.Approval.Update.Default);
            var upx_Review_Approval_UpdateFinalStateFields          = upx_Review_Approval_Update.AddUnityChild(UnitySelector.Review.Approval.Update.UpdateFinalStateFields);

            var upx_Review_AssessmentResults                        = upx_Review.AddUnityChild(UnitySelector.Review.AssessmentResults.Default);
            var upx_Review_AssessmentResults_Update                 = upx_Review_AssessmentResults.AddUnityChild(UnitySelector.Review.AssessmentResults.Update.Default);
            var upx_Review_AssessmentResults_UpdateFinalStateFields = upx_Review_AssessmentResults_Update.AddUnityChild(UnitySelector.Review.AssessmentResults.Update.UpdateFinalStateFields);

            var upx_Review_AssessmentReviewList                     = upx_Review.AddUnityChild(UnitySelector.Review.AssessmentReviewList.Default);
            var upx_Review_AssessmentReviewList_Create              = upx_Review_AssessmentReviewList.AddUnityChild(UnitySelector.Review.AssessmentReviewList.Create);

            var upx_Review_AssessmentReviewList_SendBack            = upx_Review_AssessmentReviewList.AddUnityChild(UnitySelector.Review.AssessmentReviewList.Update.SendBack);
            var upx_Review_AssessmentReviewList_Complete            = upx_Review_AssessmentReviewList.AddUnityChild(UnitySelector.Review.AssessmentReviewList.Update.Complete);
            #endregion
        }

        public static void AddApplication_ProjectInfo_Permissions(this PermissionGroupDefinition grantApplicationPermissionsGroup)
        {
            #region PROJECT INFO GRANULAR PERMISSIONS
            var upx_Project                                     = grantApplicationPermissionsGroup.AddPermission(UnitySelector.Project.Default, L(UnitySelector.Project.Default));

            var upx_Project_Summary                             = upx_Project.AddUnityChild(UnitySelector.Project.Summary.Default);
            var upx_Project_Summary_Update                      = upx_Project_Summary.AddUnityChild(UnitySelector.Project.Summary.Update.Default);
            var upx_Project_Summary_UpdateFinalStateFields      = upx_Project_Summary_Update.AddUnityChild(UnitySelector.Project.Summary.Update.UpdateFinalStateFields);

            var upx_Project_Location                            = upx_Project.AddUnityChild(UnitySelector.Project.Location.Default);
            var upx_Project_Location_Update                     = upx_Project_Location.AddUnityChild(UnitySelector.Project.Location.Update.Default);
            var upx_Project_Location_UpdateFinalStateFields     = upx_Project_Location_Update.AddUnityChild(UnitySelector.Project.Location.Update.UpdateFinalStateFields);
            #endregion
        }

        public static void AddApplication_ApplicantInfo_Permissions(this PermissionGroupDefinition grantApplicationPermissionsGroup)
        {
            #region APPLICANT INFO GRANULAR PERMISSIONS
            var upx_Applicant                                   = grantApplicationPermissionsGroup.AddPermission(UnitySelector.Applicant.Default, L(UnitySelector.Applicant.Default));

            var upx_Applicant_Summary                           = upx_Applicant.AddUnityChild(UnitySelector.Applicant.Summary.Default);
            var upx_Applicant_Summary_Update                    = upx_Applicant_Summary.AddUnityChild(UnitySelector.Applicant.Summary.Update);

            var upx_Applicant_Contact                           = upx_Applicant.AddUnityChild(UnitySelector.Applicant.Contact.Default);
            var upx_Applicant_Contact_Update                    = upx_Applicant_Contact.AddUnityChild(UnitySelector.Applicant.Contact.Update);

            var upx_Applicant_Authority                         = upx_Applicant.AddUnityChild(UnitySelector.Applicant.Authority.Default);
            var upx_Applicant_Authority_Update                  = upx_Applicant_Authority.AddUnityChild(UnitySelector.Applicant.Authority.Update);

            var upx_Applicant_Location                          = upx_Applicant.AddUnityChild(UnitySelector.Applicant.Location.Default);
            var upx_Applicant_Location_Update                   = upx_Applicant_Location.AddUnityChild(UnitySelector.Applicant.Location.Update);

            var upx_Applicant_AdditionalContact                 = upx_Applicant.AddUnityChild(UnitySelector.Applicant.AdditionalContact.Default);
            var upx_Applicant_AdditionalContact_Create          = upx_Applicant_AdditionalContact.AddUnityChild(UnitySelector.Applicant.AdditionalContact.Create);
            var upx_Applicant_AdditionalContact_Update          = upx_Applicant_AdditionalContact.AddUnityChild(UnitySelector.Applicant.AdditionalContact.Update);
            #endregion
        }

        public static PermissionDefinition AddUnityChild(this PermissionDefinition parent, string name)
        {
            return parent.AddChild(name, LocalizableString.Create<GrantManagerResource>(name));
        }

        private static LocalizableString L(string name)
        {
            return LocalizableString.Create<GrantManagerResource>(name);
        }
    }
}
