﻿using Unity.GrantManager.Localization;
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
            var dashboardPermissions = grantApplicationPermissionsGroup.AddPermission(GrantApplicationPermissions.Dashboard.Default, L("Permission:GrantApplicationManagement.Dashboard.Default"));
            dashboardPermissions.AddChild(GrantApplicationPermissions.Dashboard.ViewDashboard, L("Permission:GrantApplicationManagement.Dashboard.ViewDashboard"));

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
            var assessmentResultPermissions = grantApplicationPermissionsGroup.AddPermission(GrantApplicationPermissions.AssessmentResults.Default, L("Permission:GrantApplicationPermissions.AssessmentResults.Default"));
            var updateAssessmentResultPermissions = assessmentResultPermissions.AddChild(GrantApplicationPermissions.AssessmentResults.Edit, L("Permission:GrantApplicationPermissions.AssessmentResults.Edit"));
            updateAssessmentResultPermissions.AddChild(GrantApplicationPermissions.AssessmentResults.EditFinalStateFields, L("Permission:GrantApplicationPermissions.AssessmentResults.EditFinalStateFields"));

            // Project Info
            var projectInfoPermissions = grantApplicationPermissionsGroup.AddPermission(GrantApplicationPermissions.ProjectInfo.Default, L("Permission:GrantApplicationManagement.ProjectInfo"));
            var updateProjectInfoPermissions = projectInfoPermissions.AddChild(GrantApplicationPermissions.ProjectInfo.Update, L("Permission:GrantApplicationManagement.ProjectInfo.Update"));
            updateProjectInfoPermissions.AddChild(GrantApplicationPermissions.ProjectInfo.UpdateFinalStateFields, L("Permission:GrantApplicationManagement.ProjectInfo.Update.UpdateFinalStateFields"));

            var settingManagement = context.GetGroup(SettingManagementPermissions.GroupName);
            settingManagement.AddPermission(UnitySettingManagementPermissions.UserInterface, L("Permission:UnitySettingManagementPermissions.UserInterface"));

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
