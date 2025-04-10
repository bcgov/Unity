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
            
            var applicationResultsPermissions = assessmentResultPermissions.AddChild(UnitySelector.Review.AssessmentResults.Default, L(UnitySelector.Review.AssessmentResults.Default));
            var applicationResultsUpdatePermissions = applicationResultsPermissions.AddChild(UnitySelector.Review.AssessmentResults.Update, L(UnitySelector.Review.AssessmentResults.Update));
            applicationResultsUpdatePermissions.AddChild(GrantApplicationPermissions.AssessmentResults.EditFinalStateFields, L("Permission:GrantApplicationPermissions.AssessmentResults.EditFinalStateFields"));

            assessmentResultPermissions.AddChild(UnitySelector.Review.AssessmentReviewList.Default, L(UnitySelector.Review.AssessmentReviewList.Default));

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

        private void AddStandardPermissionGroups(PermissionGroupDefinition grantApplicationPermissionsGroup)
        {
            #region APPLICANT PERMISSIONS
            var upx_Applicant                                     = grantApplicationPermissionsGroup.AddPermission(UnitySelector.Applicant.Default, L(UnitySelector.Applicant.Default));
            var upx_Applicant_Authority                           = upx_Applicant.AddChild(UnitySelector.Applicant.Authority.Default);
            var upx_Applicant_Authority_Create                    = upx_Applicant_Authority.AddChild(UnitySelector.Applicant.Authority.Create);
            var upx_Applicant_Authority_Update                    = upx_Applicant_Authority.AddChild(UnitySelector.Applicant.Authority.Update);
            var upx_Applicant_Authority_Delete                    = upx_Applicant_Authority.AddChild(UnitySelector.Applicant.Authority.Delete);
            var upx_Applicant_Contact                             = upx_Applicant.AddChild(UnitySelector.Applicant.Contact.Default);
            var upx_Applicant_Contact_Create                      = upx_Applicant_Contact.AddChild(UnitySelector.Applicant.Contact.Create);
            var upx_Applicant_Contact_Update                      = upx_Applicant_Contact.AddChild(UnitySelector.Applicant.Contact.Update);
            var upx_Applicant_Contact_Delete                      = upx_Applicant_Contact.AddChild(UnitySelector.Applicant.Contact.Delete);
            var upx_Applicant_Location                            = upx_Applicant.AddChild(UnitySelector.Applicant.Location.Default);
            var upx_Applicant_Location_Create                     = upx_Applicant_Location.AddChild(UnitySelector.Applicant.Location.Create);
            var upx_Applicant_Location_Update                     = upx_Applicant_Location.AddChild(UnitySelector.Applicant.Location.Update);
            var upx_Applicant_Location_Delete                     = upx_Applicant_Location.AddChild(UnitySelector.Applicant.Location.Delete);
            var upx_Applicant_Summary                             = upx_Applicant.AddChild(UnitySelector.Applicant.Summary.Default);
            var upx_Applicant_Summary_Create                      = upx_Applicant_Summary.AddChild(UnitySelector.Applicant.Summary.Create);
            var upx_Applicant_Summary_Update                      = upx_Applicant_Summary.AddChild(UnitySelector.Applicant.Summary.Update);
            var upx_Applicant_Summary_Delete                      = upx_Applicant_Summary.AddChild(UnitySelector.Applicant.Summary.Delete);
            var upx_Applicant_Supplier                            = upx_Applicant.AddChild(UnitySelector.Applicant.Supplier.Default);
            var upx_Applicant_Supplier_Create                     = upx_Applicant_Supplier.AddChild(UnitySelector.Applicant.Supplier.Create);
            var upx_Applicant_Supplier_Update                     = upx_Applicant_Supplier.AddChild(UnitySelector.Applicant.Supplier.Update);
            var upx_Applicant_Supplier_Delete                     = upx_Applicant_Supplier.AddChild(UnitySelector.Applicant.Supplier.Delete);
            #endregion

            #region APPLICATION INFO PERMISSIONS
            var upx_Application                                   = grantApplicationPermissionsGroup.AddPermission(UnitySelector.Application.Default, L(UnitySelector.Application.Default));
            var upx_Application_Scoresheet                        = upx_Application.AddChild(UnitySelector.Application.Scoresheet.Default);
            var upx_Application_Scoresheet_Create                 = upx_Application_Scoresheet.AddChild(UnitySelector.Application.Scoresheet.Create);
            var upx_Application_Scoresheet_Update                 = upx_Application_Scoresheet.AddChild(UnitySelector.Application.Scoresheet.Update);
            var upx_Application_Scoresheet_Delete                 = upx_Application_Scoresheet.AddChild(UnitySelector.Application.Scoresheet.Delete);
            var upx_Application_Summary                           = upx_Application.AddChild(UnitySelector.Application.Summary.Default);
            var upx_Application_Summary_Create                    = upx_Application_Summary.AddChild(UnitySelector.Application.Summary.Create);
            var upx_Application_Summary_Update                    = upx_Application_Summary.AddChild(UnitySelector.Application.Summary.Update);
            var upx_Application_Summary_Delete                    = upx_Application_Summary.AddChild(UnitySelector.Application.Summary.Delete);
            #endregion

            #region REVIEW & ASSESSMENT PERMISSIONS
            var upx_Review                                        = grantApplicationPermissionsGroup.AddPermission(UnitySelector.Review.Default, L(UnitySelector.Review.Default));
            var upx_Review_Approval                               = upx_Review.AddChild(UnitySelector.Review.Approval.Default);
            var upx_Review_Approval_Create                        = upx_Review_Approval.AddChild(UnitySelector.Review.Approval.Create);
            var upx_Review_Approval_Update                        = upx_Review_Approval.AddChild(UnitySelector.Review.Approval.Update);
            var upx_Review_Approval_Delete                        = upx_Review_Approval.AddChild(UnitySelector.Review.Approval.Delete);
            var upx_Review_AssessmentResults                      = upx_Review.AddChild(UnitySelector.Review.AssessmentResults.Default);
            var upx_Review_AssessmentResults_Create               = upx_Review_AssessmentResults.AddChild(UnitySelector.Review.AssessmentResults.Create);
            var upx_Review_AssessmentResults_Update               = upx_Review_AssessmentResults.AddChild(UnitySelector.Review.AssessmentResults.Update);
            var upx_Review_AssessmentResults_Delete               = upx_Review_AssessmentResults.AddChild(UnitySelector.Review.AssessmentResults.Delete);
            var upx_Review_AssessmentResults_EditFinalStateFields = upx_Review.AddChild(UnitySelector.Review.AssessmentResults.EditFinalStateFields);
            var upx_Review_AssessmentReviewList                   = upx_Review.AddChild(UnitySelector.Review.AssessmentReviewList.Default);
            var upx_Review_AssessmentReviewList_Create            = upx_Review_AssessmentReviewList.AddChild(UnitySelector.Review.AssessmentReviewList.Create);
            var upx_Review_AssessmentReviewList_Update            = upx_Review_AssessmentReviewList.AddChild(UnitySelector.Review.AssessmentReviewList.Update);
            var upx_Review_AssessmentReviewList_Delete            = upx_Review_AssessmentReviewList.AddChild(UnitySelector.Review.AssessmentReviewList.Delete);
            var upx_Review_AssessmentReviewList_SendBack          = upx_Review_AssessmentReviewList.AddChild(UnitySelector.Review.AssessmentReviewList.SendBack);
            var upx_Review_AssessmentReviewList_Complete          = upx_Review_AssessmentReviewList.AddChild(UnitySelector.Review.AssessmentReviewList.Complete);
            var upx_Review_Worksheet                              = upx_Review.AddChild(UnitySelector.Review.Worksheet.Default);
            var upx_Review_Worksheet_Create                       = upx_Review_Worksheet.AddChild(UnitySelector.Review.Worksheet.Create);
            var upx_Review_Worksheet_Update                       = upx_Review_Worksheet.AddChild(UnitySelector.Review.Worksheet.Update);
            var upx_Review_Worksheet_Delete                       = upx_Review_Worksheet.AddChild(UnitySelector.Review.Worksheet.Delete);
            #endregion

            #region ATTACHMENT PERMISSIONS
            var upx_Attachment                                    = grantApplicationPermissionsGroup.AddPermission(UnitySelector.Attachment.Default, L(UnitySelector.Attachment.Default));
            var upx_Attachment_Review                             = upx_Attachment.AddChild(UnitySelector.Attachment.Review.Default);
            var upx_Attachment_Review_Create                      = upx_Attachment_Review.AddChild(UnitySelector.Attachment.Review.Create);
            var upx_Attachment_Review_Update                      = upx_Attachment_Review.AddChild(UnitySelector.Attachment.Review.Update);
            var upx_Attachment_Review_Delete                      = upx_Attachment_Review.AddChild(UnitySelector.Attachment.Review.Delete);
            var upx_Attachment_Notification                       = upx_Attachment.AddChild(UnitySelector.Attachment.Notification.Default);
            var upx_Attachment_Notification_Create                = upx_Attachment_Notification.AddChild(UnitySelector.Attachment.Notification.Create);
            var upx_Attachment_Notification_Update                = upx_Attachment_Notification.AddChild(UnitySelector.Attachment.Notification.Update);
            var upx_Attachment_Notification_Delete                = upx_Attachment_Notification.AddChild(UnitySelector.Attachment.Notification.Delete);
            var upx_Attachment_Submission                         = upx_Attachment.AddChild(UnitySelector.Attachment.Submission.Default);
            var upx_Attachment_Submission_Create                  = upx_Attachment_Submission.AddChild(UnitySelector.Attachment.Submission.Create);
            var upx_Attachment_Submission_Update                  = upx_Attachment_Submission.AddChild(UnitySelector.Attachment.Submission.Update);
            var upx_Attachment_Submission_Delete                  = upx_Attachment_Submission.AddChild(UnitySelector.Attachment.Submission.Delete);
            #endregion

            #region FUNDING PERMISSIONS
            var upx_Funding                                       = grantApplicationPermissionsGroup.AddPermission(UnitySelector.Funding.Default, L(UnitySelector.Funding.Default));
            var upx_Funding_Agreement                             = upx_Funding.AddChild(UnitySelector.Funding.Agreement.Default);
            var upx_Funding_Agreement_Create                      = upx_Funding_Agreement.AddChild(UnitySelector.Funding.Agreement.Create);
            var upx_Funding_Agreement_Update                      = upx_Funding_Agreement.AddChild(UnitySelector.Funding.Agreement.Update);
            var upx_Funding_Agreement_Delete                      = upx_Funding_Agreement.AddChild(UnitySelector.Funding.Agreement.Delete);
            #endregion

            var upx_History                                       = grantApplicationPermissionsGroup.AddPermission(UnitySelector.History.Default, L(UnitySelector.History.Default));
            
            var upx_Notification                                  = grantApplicationPermissionsGroup.AddPermission(UnitySelector.Notification.Default, L(UnitySelector.Notification.Default));
            var upx_Notification_Create                           = upx_Notification.AddChild(UnitySelector.Notification.Create);
            var upx_Notification_Update                           = upx_Notification.AddChild(UnitySelector.Notification.Update);
            var upx_Notification_Delete                           = upx_Notification.AddChild(UnitySelector.Notification.Delete);
            var upx_Notification_Draft                            = upx_Notification.AddChild(UnitySelector.Notification.Draft.Default);
            var upx_Notification_Draft_Create                     = upx_Notification_Draft.AddChild(UnitySelector.Notification.Draft.Create);
            var upx_Notification_Draft_Delete                     = upx_Notification_Draft.AddChild(UnitySelector.Notification.Draft.Delete);
            var upx_Notification_Draft_Update                     = upx_Notification_Draft.AddChild(UnitySelector.Notification.Draft.Update);

            #region PAYMENT PERMISSIONS
            var upx_Payment                                       = grantApplicationPermissionsGroup.AddPermission(UnitySelector.Payment.Default, L(UnitySelector.Payment.Default));
            var upx_Payment_Summary                               = upx_Payment.AddChild(UnitySelector.Payment.Summary.Default);
            var upx_Payment_Summary_Create                        = upx_Payment_Summary.AddChild(UnitySelector.Payment.Summary.Create);
            var upx_Payment_Summary_Update                        = upx_Payment_Summary.AddChild(UnitySelector.Payment.Summary.Update);
            var upx_Payment_Summary_Delete                        = upx_Payment_Summary.AddChild(UnitySelector.Payment.Summary.Delete);
            var upx_Payment_PaymentList                           = upx_Payment.AddChild(UnitySelector.Payment.PaymentList.Default);
            var upx_Payment_PaymentList_Create                    = upx_Payment_PaymentList.AddChild(UnitySelector.Payment.PaymentList.Create);
            var upx_Payment_PaymentList_Update                    = upx_Payment_PaymentList.AddChild(UnitySelector.Payment.PaymentList.Update);
            var upx_Payment_PaymentList_Delete                    = upx_Payment_PaymentList.AddChild(UnitySelector.Payment.PaymentList.Delete);
            #endregion

            #region PROJECT PERMISSIONS
            var upx_Project                                       = grantApplicationPermissionsGroup.AddPermission(UnitySelector.Project.Default, L(UnitySelector.Project.Default));
            var upx_Project_Location                              = upx_Project.AddChild(UnitySelector.Project.Location.Default);
            var upx_Project_Location_Create                       = upx_Project_Location.AddChild(UnitySelector.Project.Location.Create);
            var upx_Project_Location_Update                       = upx_Project_Location.AddChild(UnitySelector.Project.Location.Update);
            var upx_Project_Location_Delete                       = upx_Project_Location.AddChild(UnitySelector.Project.Location.Delete);
            var upx_Project_Summary                               = upx_Project.AddChild(UnitySelector.Project.Summary.Default);
            var upx_Project_Summary_Create                        = upx_Project_Summary.AddChild(UnitySelector.Project.Summary.Create);
            var upx_Project_Summary_Update                        = upx_Project_Summary.AddChild(UnitySelector.Project.Summary.Update);
            var upx_Project_Summary_Delete                        = upx_Project_Summary.AddChild(UnitySelector.Project.Summary.Delete);
            #endregion

            var upx_Comment                                       = grantApplicationPermissionsGroup.AddPermission(UnitySelector.Comment.Default, L(UnitySelector.Comment.Default));
            var upx_Flex                                          = grantApplicationPermissionsGroup.AddPermission(UnitySelector.Flex.Default, L(UnitySelector.Flex.Default));
        }

        private static LocalizableString L(string name)
        {
            return LocalizableString.Create<GrantManagerResource>(name);
        }
    }
}
