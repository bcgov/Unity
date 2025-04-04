using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Permissions;
using Volo.Abp.Uow;

namespace Unity.GrantManager.GrantApplications
{
    [Authorize]
    public class ApplicationApprovalService(IApplicationRepository applicationRepository,
        IGrantApplicationAppService grantApplicationsService, // Services should not inject services in same module!
        IUnitOfWorkManager unitofWorkManager) : GrantManagerAppService, IApplicationApprovalService
    {
        /// <summary>
        /// Bulk approve applications
        /// </summary>
        /// <param name="applicationGuids"></param>
        /// <returns></returns>
        public async Task<bool> BulkApproveApplications(Guid[] applicationGuids)
        {
            // We read and write individually here to make sure all applications trigger ther approval correctly as a best effort per application
            foreach (var applicationId in applicationGuids)
            {
                try
                {
                    using var uow = unitofWorkManager.Begin(requiresNew: true);
                    await grantApplicationsService.TriggerAction(applicationId, GrantApplicationAction.Approve);
                    await uow.CompleteAsync();
                }
                catch (Exception ex)
                {
                    // Log the error and continue with the next application
                    Logger.LogError(ex, "Error approving application with ID: {ApplicationId}", applicationId);
                    // Add to error list or handle as needed
                }
            }

            return await Task.FromResult(true);
        }

        /// <summary>
        /// Get applications for bulk approval with addeded on validation information
        /// </summary>
        /// <param name="applicationGuids"></param>
        /// <returns></returns>
        public async Task<List<GrantApplicationBatchApprovalDto>> GetApplicationsForBulkApproval(Guid[] applicationGuids)
        {
            var applications = await applicationRepository.GetListByIdsAsync(applicationGuids);
            return await ValidateBulkApplications([.. applications]);
        }


        /// <summary>
        /// Add validations to the applications
        /// </summary>
        /// <param name="applications"></param>
        /// <returns></returns>
        private async Task<List<GrantApplicationBatchApprovalDto>> ValidateBulkApplications(Application[] applications)
        {
            var applicationsForApproval = new List<GrantApplicationBatchApprovalDto>();

            foreach (var application in applications)
            {
                List<string> validationMessages = await RunValidations(application);

                applicationsForApproval.Add(new GrantApplicationBatchApprovalDto()
                {
                    ApplicationId = application.Id,
                    ApprovedAmount = application.ApprovedAmount,
                    RequestedAmount = application.RequestedAmount,
                    FinalDecisionDate = application.FinalDecisionDate,
                    ReferenceNo = application.ReferenceNo,
                    ValidationMessages = validationMessages,
                    ApplicantName = application.Applicant.ApplicantName ?? string.Empty
                });
            }

            return applicationsForApproval;
        }

        /// <summary>
        /// Run the validations for the application
        /// </summary>
        /// <param name="application"></param>
        /// <returns></returns>
        private async Task<List<string>> RunValidations(Application application)
        {
            var validWorkflow = MeetsWorkflowRequirement(application, GrantApplicationAction.Approve);
            var authorized = await MeetsAuthorizationRequirement(application, GrantApplicationAction.Approve);
            var validationMessages = new List<string>();

            if (!validWorkflow)
                validationMessages.Add("Invalid workflow status for approval.");
            if (!authorized)
                validationMessages.Add("Insufficient permissions for approval.");

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
