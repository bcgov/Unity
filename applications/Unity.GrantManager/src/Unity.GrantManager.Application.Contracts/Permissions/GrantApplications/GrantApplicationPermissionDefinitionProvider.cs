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

            // Assessments
            var assessmentPermissions = grantApplicationPermissionsGroup.AddPermission(GrantApplicationPermissions.Assessments.Default, L("Permission:GrantApplicationPermissions.Assessments.Default"));
            assessmentPermissions.AddChild(GrantApplicationPermissions.Assessments.Create, L("Permission:GrantApplicationPermissions.Assessments.Create"));
            assessmentPermissions.AddChild(GrantApplicationPermissions.Assessments.SendBack, L("Permission:GrantApplicationPermissions.Assessments.SendBack"));
            assessmentPermissions.AddChild(GrantApplicationPermissions.Assessments.Confirm, L("Permission:GrantApplicationPermissions.Assessments.Confirm"));

            // Assessment Results
            var assessmentResultPermissions = grantApplicationPermissionsGroup.AddPermission(GrantApplicationPermissions.AssessmentResults.Default, L(UnitySelector.Review.Default));

            var assessmentResultApprovalPermissions = assessmentResultPermissions.AddChild(UnitySelector.Review.Approval.Default,L(UnitySelector.Review.Approval.Default));
            assessmentResultApprovalPermissions.AddChild(UnitySelector.Review.Approval.Update, L(UnitySelector.Review.Approval.Update));
            
            var applicationResultsPermissions = assessmentResultPermissions.AddChild(UnitySelector.Review.Results.Default, L(UnitySelector.Review.Results.Default));
            var applicationResultsUpdatePermissions = applicationResultsPermissions.AddChild(UnitySelector.Review.Results.Update, L(UnitySelector.Review.Results.Update));
            applicationResultsUpdatePermissions.AddChild(GrantApplicationPermissions.AssessmentResults.EditFinalStateFields, L("Permission:GrantApplicationPermissions.AssessmentResults.EditFinalStateFields"));

            assessmentResultPermissions.AddChild(UnitySelector.Review.ReviewList.Default, L(UnitySelector.Review.ReviewList.Default));

            var updateAssessmentResultPermissions = assessmentResultPermissions.AddChild(GrantApplicationPermissions.AssessmentResults.Edit, L("Permission:GrantApplicationPermissions.AssessmentResults.Edit"));

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
}
