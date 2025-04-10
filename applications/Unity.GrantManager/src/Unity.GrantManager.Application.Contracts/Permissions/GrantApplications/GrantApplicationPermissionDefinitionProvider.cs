using Microsoft.CodeAnalysis.CSharp.Syntax;
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

            grantApplicationPermissionsGroup.AddApplication_ReviewAndAssessment_Permissions();

            //grantApplicationPermissionsGroup.AddApplication_ApplicantInfo_Permissions();
            //grantApplicationPermissionsGroup.AddApplication_ProjectInfo_Permissions();
            //grantApplicationPermissionsGroup.AddApplication_FundingAgreement_Permissions();

            //grantApplicationPermissionsGroup.AddApplicationInfoPermissions();
            //grantApplicationPermissionsGroup.AddApplicationPaymentPermissions();
            //grantApplicationPermissionsGroup.AddApplicationNotificationPermissions();


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

            //// Assessment Results
            //var assessmentResultPermissions = grantApplicationPermissionsGroup.AddPermission(GrantApplicationPermissions.AssessmentResults.Default, L(UnitySelector.Review.Default));

            //var assessmentResultApprovalPermissions = assessmentResultPermissions.AddUnityChild(UnitySelector.Review.Approval.Default,L(UnitySelector.Review.Approval.Default));
            //assessmentResultApprovalPermissions.AddUnityChild(UnitySelector.Review.Approval.Update, L(UnitySelector.Review.Approval.Update));

            //var applicationResultsPermissions = assessmentResultPermissions.AddUnityChild(UnitySelector.Review.AssessmentResults.Default, L(UnitySelector.Review.AssessmentResults.Default));
            //var applicationResultsUpdatePermissions = applicationResultsPermissions.AddUnityChild(UnitySelector.Review.AssessmentResults.Update, L(UnitySelector.Review.AssessmentResults.Update));
            //applicationResultsUpdatePermissions.AddChild(GrantApplicationPermissions.AssessmentResults.EditFinalStateFields, L("Permission:GrantApplicationPermissions.AssessmentResults.EditFinalStateFields"));

            //assessmentResultPermissions.AddUnityChild(UnitySelector.Review.AssessmentReviewList.Default, L(UnitySelector.Review.AssessmentReviewList.Default));

            //var updateAssessmentResultPermissions = assessmentResultPermissions.AddChild(GrantApplicationPermissions.AssessmentResults.Edit, L("Permission:GrantApplicationPermissions.AssessmentResults.Edit"));

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
            // TODO: Move attachments to individual resource groups
            #region ATTACHMENT PERMISSIONS
            //var upx_Attachment                                    = grantApplicationPermissionsGroup.AddPermission(UnitySelector.Attachment.Default, L(UnitySelector.Attachment.Default));
            //var upx_Attachment_Review                             = upx_Attachment.AddUnityChild(UnitySelector.Attachment.Review.Default);
            //var upx_Attachment_Review_Create                      = upx_Attachment_Review.AddUnityChild(UnitySelector.Attachment.Review.Create);
            //var upx_Attachment_Review_Update                      = upx_Attachment_Review.AddUnityChild(UnitySelector.Attachment.Review.Update);
            //var upx_Attachment_Review_Delete                      = upx_Attachment_Review.AddUnityChild(UnitySelector.Attachment.Review.Delete);
            //var upx_Attachment_Notification                       = upx_Attachment.AddUnityChild(UnitySelector.Attachment.Notification.Default);
            //var upx_Attachment_Notification_Create                = upx_Attachment_Notification.AddUnityChild(UnitySelector.Attachment.Notification.Create);
            //var upx_Attachment_Notification_Update                = upx_Attachment_Notification.AddUnityChild(UnitySelector.Attachment.Notification.Update);
            //var upx_Attachment_Notification_Delete                = upx_Attachment_Notification.AddUnityChild(UnitySelector.Attachment.Notification.Delete);
            //var upx_Attachment_Submission                         = upx_Attachment.AddUnityChild(UnitySelector.Attachment.Submission.Default);
            //var upx_Attachment_Submission_Create                  = upx_Attachment_Submission.AddUnityChild(UnitySelector.Attachment.Submission.Create);
            //var upx_Attachment_Submission_Update                  = upx_Attachment_Submission.AddUnityChild(UnitySelector.Attachment.Submission.Update);
            //var upx_Attachment_Submission_Delete                  = upx_Attachment_Submission.AddUnityChild(UnitySelector.Attachment.Submission.Delete);
            #endregion

            var upx_History                                       = grantApplicationPermissionsGroup.AddPermission(UnitySelector.History.Default, L(UnitySelector.History.Default));
            var upx_Comment                                       = grantApplicationPermissionsGroup.AddPermission(UnitySelector.Comment.Default, L(UnitySelector.Comment.Default));
            var upx_Flex                                          = grantApplicationPermissionsGroup.AddPermission(UnitySelector.Flex.Default, L(UnitySelector.Flex.Default));
        }

        private static LocalizableString L(string name)
        {
            return LocalizableString.Create<GrantManagerResource>(name);
        }
    }

    public static class PermissionDefinitionExtensions
    {
        public static PermissionDefinition AddUnityChild(this PermissionDefinition parent, string name)
        {
            return parent.AddChild(name, LocalizableString.Create<GrantManagerResource>(name));
        }
    }

    public static class PermissionGroupDefinitionExtensions
    {
        public static void AddApplication_ReviewAndAssessment_Permissions(this PermissionGroupDefinition grantApplicationPermissionsGroup)
        {
            #region REVIEW & ASSESSMENT PERMISSIONS
            var upx_Review                                        = grantApplicationPermissionsGroup.AddPermission(UnitySelector.Review.Default, L(UnitySelector.Review.Default));
            var upx_Review_Approval                               = upx_Review.AddUnityChild(UnitySelector.Review.Approval.Default);
            var upx_Review_Approval_Create                        = upx_Review_Approval.AddUnityChild(UnitySelector.Review.Approval.Create);
            var upx_Review_Approval_Update                        = upx_Review_Approval.AddUnityChild(UnitySelector.Review.Approval.Update);
            var upx_Review_Approval_Delete                        = upx_Review_Approval.AddUnityChild(UnitySelector.Review.Approval.Delete);
            var upx_Review_AssessmentResults                      = upx_Review.AddUnityChild(UnitySelector.Review.AssessmentResults.Default);
            var upx_Review_AssessmentResults_Create               = upx_Review_AssessmentResults.AddUnityChild(UnitySelector.Review.AssessmentResults.Create);
            var upx_Review_AssessmentResults_Update               = upx_Review_AssessmentResults.AddUnityChild(UnitySelector.Review.AssessmentResults.Update);
            var upx_Review_AssessmentResults_Delete               = upx_Review_AssessmentResults.AddUnityChild(UnitySelector.Review.AssessmentResults.Delete);
            var upx_Review_AssessmentResults_EditFinalStateFields = upx_Review.AddUnityChild(UnitySelector.Review.AssessmentResults.EditFinalStateFields);
            var upx_Review_AssessmentReviewList                   = upx_Review.AddUnityChild(UnitySelector.Review.AssessmentReviewList.Default);
            var upx_Review_AssessmentReviewList_Create            = upx_Review_AssessmentReviewList.AddUnityChild(UnitySelector.Review.AssessmentReviewList.Create);
            var upx_Review_AssessmentReviewList_Update            = upx_Review_AssessmentReviewList.AddUnityChild(UnitySelector.Review.AssessmentReviewList.Update);
            var upx_Review_AssessmentReviewList_Delete            = upx_Review_AssessmentReviewList.AddUnityChild(UnitySelector.Review.AssessmentReviewList.Delete);
            var upx_Review_AssessmentReviewList_SendBack          = upx_Review_AssessmentReviewList.AddUnityChild(UnitySelector.Review.AssessmentReviewList.SendBack);
            var upx_Review_AssessmentReviewList_Complete          = upx_Review_AssessmentReviewList.AddUnityChild(UnitySelector.Review.AssessmentReviewList.Complete);
            var upx_Review_Worksheet                              = upx_Review.AddUnityChild(UnitySelector.Review.Worksheet.Default);
            var upx_Review_Worksheet_Create                       = upx_Review_Worksheet.AddUnityChild(UnitySelector.Review.Worksheet.Create);
            var upx_Review_Worksheet_Update                       = upx_Review_Worksheet.AddUnityChild(UnitySelector.Review.Worksheet.Update);
            var upx_Review_Worksheet_Delete                       = upx_Review_Worksheet.AddUnityChild(UnitySelector.Review.Worksheet.Delete);
            #endregion
        }

        public static void AddApplication_ApplicantInfo_Permissions(this PermissionGroupDefinition grantApplicationPermissionsGroup)
        {
            #region APPLICANT PERMISSIONS
            var upx_Applicant                                     = grantApplicationPermissionsGroup.AddPermission(UnitySelector.Applicant.Default, L(UnitySelector.Applicant.Default));
            var upx_Applicant_Authority                           = upx_Applicant.AddUnityChild(UnitySelector.Applicant.Authority.Default);
            var upx_Applicant_Authority_Create                    = upx_Applicant_Authority.AddUnityChild(UnitySelector.Applicant.Authority.Create);
            var upx_Applicant_Authority_Update                    = upx_Applicant_Authority.AddUnityChild(UnitySelector.Applicant.Authority.Update);
            var upx_Applicant_Authority_Delete                    = upx_Applicant_Authority.AddUnityChild(UnitySelector.Applicant.Authority.Delete);
            var upx_Applicant_Contact                             = upx_Applicant.AddUnityChild(UnitySelector.Applicant.Contact.Default);
            var upx_Applicant_Contact_Create                      = upx_Applicant_Contact.AddUnityChild(UnitySelector.Applicant.Contact.Create);
            var upx_Applicant_Contact_Update                      = upx_Applicant_Contact.AddUnityChild(UnitySelector.Applicant.Contact.Update);
            var upx_Applicant_Contact_Delete                      = upx_Applicant_Contact.AddUnityChild(UnitySelector.Applicant.Contact.Delete);
            var upx_Applicant_Location                            = upx_Applicant.AddUnityChild(UnitySelector.Applicant.Location.Default);
            var upx_Applicant_Location_Create                     = upx_Applicant_Location.AddUnityChild(UnitySelector.Applicant.Location.Create);
            var upx_Applicant_Location_Update                     = upx_Applicant_Location.AddUnityChild(UnitySelector.Applicant.Location.Update);
            var upx_Applicant_Location_Delete                     = upx_Applicant_Location.AddUnityChild(UnitySelector.Applicant.Location.Delete);
            var upx_Applicant_Summary                             = upx_Applicant.AddUnityChild(UnitySelector.Applicant.Summary.Default);
            var upx_Applicant_Summary_Create                      = upx_Applicant_Summary.AddUnityChild(UnitySelector.Applicant.Summary.Create);
            var upx_Applicant_Summary_Update                      = upx_Applicant_Summary.AddUnityChild(UnitySelector.Applicant.Summary.Update);
            var upx_Applicant_Summary_Delete                      = upx_Applicant_Summary.AddUnityChild(UnitySelector.Applicant.Summary.Delete);
            var upx_Applicant_Supplier                            = upx_Applicant.AddUnityChild(UnitySelector.Applicant.Supplier.Default);
            var upx_Applicant_Supplier_Create                     = upx_Applicant_Supplier.AddUnityChild(UnitySelector.Applicant.Supplier.Create);
            var upx_Applicant_Supplier_Update                     = upx_Applicant_Supplier.AddUnityChild(UnitySelector.Applicant.Supplier.Update);
            var upx_Applicant_Supplier_Delete                     = upx_Applicant_Supplier.AddUnityChild(UnitySelector.Applicant.Supplier.Delete);
            #endregion
        }

        // TODO: REVIEW
        public static void AddApplicationInfoPermissions(this PermissionGroupDefinition grantApplicationPermissionsGroup)
        {
            #region APPLICATION INFO PERMISSIONS
            var upx_Application                                   = grantApplicationPermissionsGroup.AddPermission(UnitySelector.Application.Default, L(UnitySelector.Application.Default));
            var upx_Application_Scoresheet                        = upx_Application.AddUnityChild(UnitySelector.Application.Scoresheet.Default);
            var upx_Application_Scoresheet_Create                 = upx_Application_Scoresheet.AddUnityChild(UnitySelector.Application.Scoresheet.Create);
            var upx_Application_Scoresheet_Update                 = upx_Application_Scoresheet.AddUnityChild(UnitySelector.Application.Scoresheet.Update);
            var upx_Application_Scoresheet_Delete                 = upx_Application_Scoresheet.AddUnityChild(UnitySelector.Application.Scoresheet.Delete);
            var upx_Application_Summary                           = upx_Application.AddUnityChild(UnitySelector.Application.Summary.Default);
            var upx_Application_Summary_Create                    = upx_Application_Summary.AddUnityChild(UnitySelector.Application.Summary.Create);
            var upx_Application_Summary_Update                    = upx_Application_Summary.AddUnityChild(UnitySelector.Application.Summary.Update);
            var upx_Application_Summary_Delete                    = upx_Application_Summary.AddUnityChild(UnitySelector.Application.Summary.Delete);
            #endregion
        }

        public static void AddApplication_FundingAgreement_Permissions(this PermissionGroupDefinition grantApplicationPermissionsGroup)
        {
            #region FUNDING PERMISSIONS
            var upx_Funding                                       = grantApplicationPermissionsGroup.AddPermission(UnitySelector.Funding.Default, L(UnitySelector.Funding.Default));
            var upx_Funding_Agreement                             = upx_Funding.AddUnityChild(UnitySelector.Funding.Agreement.Default);
            var upx_Funding_Agreement_Create                      = upx_Funding_Agreement.AddUnityChild(UnitySelector.Funding.Agreement.Create);
            var upx_Funding_Agreement_Update                      = upx_Funding_Agreement.AddUnityChild(UnitySelector.Funding.Agreement.Update);
            var upx_Funding_Agreement_Delete                      = upx_Funding_Agreement.AddUnityChild(UnitySelector.Funding.Agreement.Delete);
            #endregion
        }

        public static void AddApplication_ProjectInfo_Permissions(this PermissionGroupDefinition grantApplicationPermissionsGroup)
        {
            #region PROJECT PERMISSIONS
            var upx_Project                                       = grantApplicationPermissionsGroup.AddPermission(UnitySelector.Project.Default, L(UnitySelector.Project.Default));
            var upx_Project_Location                              = upx_Project.AddUnityChild(UnitySelector.Project.Location.Default);
            var upx_Project_Location_Create                       = upx_Project_Location.AddUnityChild(UnitySelector.Project.Location.Create);
            var upx_Project_Location_Update                       = upx_Project_Location.AddUnityChild(UnitySelector.Project.Location.Update);
            var upx_Project_Location_Delete                       = upx_Project_Location.AddUnityChild(UnitySelector.Project.Location.Delete);
            var upx_Project_Summary                               = upx_Project.AddUnityChild(UnitySelector.Project.Summary.Default);
            var upx_Project_Summary_Create                        = upx_Project_Summary.AddUnityChild(UnitySelector.Project.Summary.Create);
            var upx_Project_Summary_Update                        = upx_Project_Summary.AddUnityChild(UnitySelector.Project.Summary.Update);
            var upx_Project_Summary_Delete                        = upx_Project_Summary.AddUnityChild(UnitySelector.Project.Summary.Delete);
            #endregion
        }

        public static void AddApplicationNotificationPermissions(this PermissionGroupDefinition grantApplicationPermissionsGroup)
        {
            #region NOTIFICATION PERMISSIONS
            var upx_Notification                                  = grantApplicationPermissionsGroup.AddPermission(UnitySelector.Notification.Default, L(UnitySelector.Notification.Default));
            var upx_Notification_Create                           = upx_Notification.AddUnityChild(UnitySelector.Notification.Create);
            var upx_Notification_Update                           = upx_Notification.AddUnityChild(UnitySelector.Notification.Update);
            var upx_Notification_Delete                           = upx_Notification.AddUnityChild(UnitySelector.Notification.Delete);
            var upx_Notification_Draft                            = upx_Notification.AddUnityChild(UnitySelector.Notification.Draft.Default);
            var upx_Notification_Draft_Create                     = upx_Notification_Draft.AddUnityChild(UnitySelector.Notification.Draft.Create);
            var upx_Notification_Draft_Delete                     = upx_Notification_Draft.AddUnityChild(UnitySelector.Notification.Draft.Delete);
            var upx_Notification_Draft_Update                     = upx_Notification_Draft.AddUnityChild(UnitySelector.Notification.Draft.Update);
            #endregion
        }

        public static void AddApplicationPaymentPermissions(this PermissionGroupDefinition grantApplicationPermissionsGroup)
        {
            #region PAYMENT PERMISSIONS
            var upx_Payment                                       = grantApplicationPermissionsGroup.AddPermission(UnitySelector.Payment.Default, L(UnitySelector.Payment.Default));
            var upx_Payment_Summary                               = upx_Payment.AddUnityChild(UnitySelector.Payment.Summary.Default);
            var upx_Payment_Summary_Create                        = upx_Payment_Summary.AddUnityChild(UnitySelector.Payment.Summary.Create);
            var upx_Payment_Summary_Update                        = upx_Payment_Summary.AddUnityChild(UnitySelector.Payment.Summary.Update);
            var upx_Payment_Summary_Delete                        = upx_Payment_Summary.AddUnityChild(UnitySelector.Payment.Summary.Delete);
            var upx_Payment_PaymentList                           = upx_Payment.AddUnityChild(UnitySelector.Payment.PaymentList.Default);
            var upx_Payment_PaymentList_Create                    = upx_Payment_PaymentList.AddUnityChild(UnitySelector.Payment.PaymentList.Create);
            var upx_Payment_PaymentList_Update                    = upx_Payment_PaymentList.AddUnityChild(UnitySelector.Payment.PaymentList.Update);
            var upx_Payment_PaymentList_Delete                    = upx_Payment_PaymentList.AddUnityChild(UnitySelector.Payment.PaymentList.Delete);
            #endregion
        }

        private static LocalizableString L(string name)
        {
            return LocalizableString.Create<GrantManagerResource>(name);
        }
    }
}
