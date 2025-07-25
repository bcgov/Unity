﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Events;
using Unity.GrantManager.Permissions;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Uow;

namespace Unity.GrantManager.GrantApplications
{
    [Authorize]
    public class BulkApprovalsAppService(IApplicationRepository applicationRepository,
        IApplicationManager applicationManager,
        ILocalEventBus localEventBus,
        IUnitOfWorkManager unitofWorkManager) : GrantManagerAppService, IBulkApprovalsAppService
    {
        /// <summary>
        /// Bulk approve applications
        /// </summary>
        /// <param name="batchApplicationsToApprove"></param>
        /// <returns></returns>
        public async Task<BulkApprovalResultDto> BulkApproveApplications(List<BulkApprovalDto> batchApplicationsToApprove)
        {
            var bulkApprovalResult = new BulkApprovalResultDto();

            // Need to Look at refactoring this into the single control flow for workflow approvals
            var approvalAction = GrantApplicationAction.Approve;

            // We read and write individually here to make sure all applications trigger ther approval correctly as a best effort per application
            foreach (var applicationToUpdateAndApprove in batchApplicationsToApprove)
            {
                Application? application = null;

                try
                {
                    // Fields to update
                    using var uowFields = unitofWorkManager.Begin(requiresNew: true);
                    application = await applicationRepository.GetAsync(applicationToUpdateAndApprove.ApplicationId);

                    // Manually map fixed fields directly from Application
                    applicationToUpdateAndApprove.RequestedAmount = application.RequestedAmount;
                    applicationToUpdateAndApprove.RecommendedAmount = application.RecommendedAmount;
                    applicationToUpdateAndApprove.IsDirectApproval = application.ApplicationForm?.IsDirectApproval ?? false;

                    application.ValidateDirectApprovalRecommendedAmount(applicationToUpdateAndApprove.RecommendedAmount, applicationToUpdateAndApprove.IsDirectApproval);

                    application.ValidateAndSetFinalDecisionDate(applicationToUpdateAndApprove.FinalDecisionDate);
                    application.ValidateAndSetApprovedAmount(applicationToUpdateAndApprove.ApprovedAmount);                                        

                    if (!await AuthorizationService.IsGrantedAsync(application, GetActionAuthorizationRequirement(GrantApplicationAction.Approve)))
                    {
                        throw new UnauthorizedAccessException();
                    }

                    _ = await applicationManager.TriggerAction(application.Id, GrantApplicationAction.Approve);

                    await localEventBus.PublishAsync(
                        new ApplicationChangedEvent
                        {
                            Action = approvalAction,
                            ApplicationId = application.Id
                        }
                    );

                    await uowFields.CompleteAsync();

                    bulkApprovalResult.Successes.Add(application.ReferenceNo);
                }
                catch (Exception ex)
                {
                    // Log the error and continue with the next application
                    Logger.LogError(ex, "Error approving application with ID: {ApplicationId} and ReferenceNo: {ReferenceNo}",
                        applicationToUpdateAndApprove.ApplicationId,
                        applicationToUpdateAndApprove.ReferenceNo);

                    // Add to error list or handle as needed
                    bulkApprovalResult.Failures.Add(new KeyValuePair<string, string>(application?.ReferenceNo ?? string.Empty, ex.Message));
                }
            }

            return bulkApprovalResult;
        }

        /// <summary>
        /// Get applications for bulk approval with addeded on validation information
        /// </summary>
        /// <param name="applicationGuids"></param>
        /// <returns></returns>
        public async Task<List<BulkApprovalDto>> GetApplicationsForBulkApproval(Guid[] applicationGuids)
        {
            var applications = await applicationRepository.GetListByIdsAsync(applicationGuids);
            return await ValidateBulkApplications([.. applications]);
        }


        /// <summary>
        /// Add validations to the applications
        /// </summary>
        /// <param name="applications"></param>
        /// <returns></returns>
        private async Task<List<BulkApprovalDto>> ValidateBulkApplications(Application[] applications)
        {
            var applicationsForApproval = new List<BulkApprovalDto>();

            foreach (var application in applications)
            {
                applicationsForApproval
                    .Add(MapBulkApproval(application, await RunValidations(application)));
            }

            return applicationsForApproval;
        }


        /// <summary>
        /// Map the application to a BulkApprovalDto with validation messages
        /// </summary>
        /// <param name="application"></param>
        /// <param name="validationMessages"></param>
        /// <returns></returns>
        private static BulkApprovalDto MapBulkApproval(Application application, List<(bool, string)> validationMessages)
        {
            return new BulkApprovalDto()
            {
                ApplicationId = application.Id,
                ApprovedAmount = application.ApprovedAmount,
                RequestedAmount = application.RequestedAmount,
                FinalDecisionDate = application.FinalDecisionDate,
                ReferenceNo = application.ReferenceNo,
                ValidationMessages = validationMessages.Select(s => s.Item2).ToList(),
                ApplicantName = application.Applicant.ApplicantName ?? string.Empty,
                ApplicationStatus = application.ApplicationStatus.InternalStatus,
                FormName = application.ApplicationForm?.ApplicationFormName ?? string.Empty,
                IsValid = !validationMessages.Exists(s => s.Item1),
                IsDirectApproval = application.ApplicationForm?.IsDirectApproval,
                RecommendedAmount = application.RecommendedAmount
            };
        }

        /// <summary>
        /// Run the validations for the application
        /// </summary>
        /// <param name="application"></param>
        /// <returns>A tuple with validation messages and if it should trigger a invalid state for the record</returns>
        private async Task<List<(bool, string)>> RunValidations(Application application)
        {
            var validWorkflow = MeetsWorkflowRequirement(application, GrantApplicationAction.Approve);
            var authorized = await MeetsAuthorizationRequirement(application, GrantApplicationAction.Approve);
            var validationMessages = new List<(bool, string)>();

            if (!validWorkflow)
                validationMessages.Add(new(true, "INVALID_STATUS"));
            if (!authorized)
                validationMessages.Add(new(true, "INVALID_PERMISSIONS"));

            // If this application belongs to a direct approval form, we default to RequestedAmount if the ApprovedAmount is 0.00
            if (application.ApplicationForm?.IsDirectApproval == true)
            {
                application.ApprovedAmount = application.ApprovedAmount == 0m ? application.RequestedAmount : application.ApprovedAmount;
            }
            else
            {
                // If this application does not belong to a direct approval form, ensure that RecommendedAmount is not 0.00.                                    
                if (application.RecommendedAmount == 0m)
                {
                    validationMessages.Add(new(false, "INVALID_RECOMMENDED_AMOUNT"));
                }

                // If ApprovedAmount is 0.00, we default it to RecommendedAmount
                application.ApprovedAmount = application.ApprovedAmount == 0m ? application.RecommendedAmount : application.ApprovedAmount;
            }

            // If approved amount is still 0.00 after default sets then it is an error
            if (application.ApprovedAmount == 0m)
            {
                validationMessages.Add(new(false, "INVALID_APPROVED_AMOUNT"));
            }

            return validationMessages;
        }

        /// <summary>
        /// Inline explicit validation of status check for bulk application approval
        /// </summary>
        /// <param name="application"></param>
        /// <param name="triggerAction"></param>
        /// <returns></returns>
        private static bool MeetsWorkflowRequirement(Application application, GrantApplicationAction triggerAction)
        {
            if (triggerAction != GrantApplicationAction.Approve)
            {
                return false;
            }

            // Specific ruleset for the Is Direct Approval flow
            if (application.ApplicationForm.IsDirectApproval)
            {
                if (application.ApplicationStatus.StatusCode == GrantApplicationState.GRANT_APPROVED)
                {
                    return false;
                }

                return true;
            }

            if (application.ApplicationStatus.StatusCode != GrantApplicationState.ASSESSMENT_COMPLETED)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Inline explicit validation of status check for bulk application approval
        /// </summary>
        /// <param name="application"></param>
        /// <param name="triggerAction"></param>
        /// <returns></returns>
        private async Task<bool> MeetsAuthorizationRequirement(Application application, GrantApplicationAction triggerAction)
        {
            if (!await AuthorizationService.IsGrantedAsync(application, GetActionAuthorizationRequirement(triggerAction)))
            {
                Logger.LogWarning("Approval requested for application with insufficient permissions: {ApplicationId}", application.Id);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check the authorization requirement
        /// </summary>
        /// <param name="triggerAction"></param>
        /// <returns></returns>
        private static OperationAuthorizationRequirement GetActionAuthorizationRequirement(GrantApplicationAction triggerAction)
        {
            return new OperationAuthorizationRequirement { Name = $"{GrantApplicationPermissions.Applications.Default}.{triggerAction}" };
            // this should allow anyone for now, but needs to change to a specific permission
        }
    }
}
