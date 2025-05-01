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

            // Comments
            var appCommentPermissions = grantApplicationPermissionsGroup.AddPermission(GrantApplicationPermissions.Comments.Default, L("Permission:GrantApplicationManagement.Comments.Default"));
            appCommentPermissions.AddChild(GrantApplicationPermissions.Comments.Add, L("Permission:GrantApplicationManagement.Comments.Add"));

            //-- REVIEW & ASSESSMENT PERMISSIONS
            grantApplicationPermissionsGroup.AddApplication_ReviewAndAssessment_Permissions();

            // Applicant Info
            var applicantInfoPermissions = grantApplicationPermissionsGroup.AddPermission(GrantApplicationPermissions.ApplicantInfo.Default, L($"Permission:{GrantApplicationPermissions.ApplicantInfo.Default}"));
            applicantInfoPermissions.AddChild(GrantApplicationPermissions.ApplicantInfo.Update, L($"Permission:{GrantApplicationPermissions.ApplicantInfo.Update}"));

            // Project Info
            var projectInfoPermissions = grantApplicationPermissionsGroup.AddPermission(GrantApplicationPermissions.ProjectInfo.Default, L("Permission:GrantApplicationManagement.ProjectInfo"));
            var updateProjectInfoPermissions = projectInfoPermissions.AddChild(GrantApplicationPermissions.ProjectInfo.Update, L("Permission:GrantApplicationManagement.ProjectInfo.Update"));
            updateProjectInfoPermissions.AddChild(GrantApplicationPermissions.ProjectInfo.UpdateFinalStateFields, L("Permission:GrantApplicationManagement.ProjectInfo.Update.UpdateFinalStateFields"));

            var settingManagement = context.GetGroup(SettingManagementPermissions.GroupName);
            settingManagement.AddPermission(UnitySettingManagementPermissions.UserInterface, L("Permission:UnitySettingManagementPermissions.UserInterface"));
            settingManagement.AddPermission(UnitySettingManagementPermissions.BackgroundJobSettings, L("Permission:UnitySettingManagementPermissions.BackgroundJobs"));

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
            
            // Assessment Review Transitions are implied update functions but not in the update hierarchy at this time
            // var upx_Review_AssessmentReviewList_Update           = upx_Review_AssessmentReviewList.AddUnityChild(UnitySelector.Review.AssessmentReviewList.Update.Default);
            var upx_Review_AssessmentReviewList_SendBack            = upx_Review_AssessmentReviewList.AddUnityChild(UnitySelector.Review.AssessmentReviewList.Update.SendBack);
            var upx_Review_AssessmentReviewList_Complete            = upx_Review_AssessmentReviewList.AddUnityChild(UnitySelector.Review.AssessmentReviewList.Update.Complete);

            //var upx_Review_Worksheet                                = upx_Review.AddUnityChild(UnitySelector.Review.Worksheet.Default);
            //var upx_Review_Worksheet_Update                         = upx_Review_Worksheet.AddUnityChild(UnitySelector.Review.Worksheet.Update);
            #endregion

            // Available Permission Hooks
            // var upx_Review_Approval_Create                          = upx_Review_Approval.AddUnityChild(UnitySelector.Review.Approval.Create);
            // var upx_Review_Approval_Delete                          = upx_Review_Approval.AddUnityChild(UnitySelector.Review.Approval.Delete);
            // var upx_Review_AssessmentResults_Create                 = upx_Review_AssessmentResults.AddUnityChild(UnitySelector.Review.AssessmentResults.Create);
            // var upx_Review_AssessmentResults_Delete                 = upx_Review_AssessmentResults.AddUnityChild(UnitySelector.Review.AssessmentResults.Delete);
            // var upx_Review_AssessmentReviewList_Delete              = upx_Review_AssessmentReviewList.AddUnityChild(UnitySelector.Review.AssessmentReviewList.Delete);
            // var upx_Review_Worksheet_Create                         = upx_Review_Worksheet.AddUnityChild(UnitySelector.Review.Worksheet.Create);
            // var upx_Review_Worksheet_Delete                         = upx_Review_Worksheet.AddUnityChild(UnitySelector.Review.Worksheet.Delete);
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
