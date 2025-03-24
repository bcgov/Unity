using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Permissions;

namespace Unity.GrantManager.GrantApplications
{
    [Authorize]
    public class ApplicationApprovalService(IApplicationRepository applicationRepository) : GrantManagerAppService, IApplicationApprovalService
    {
        public Task<bool> BulkApproveApplications(Guid[] applicationGuids)
        {
            throw new NotImplementedException();
        }

        public async Task<List<ApplicationApprovalDto>> GetApplicationsForBulkApproval(Guid[] applicationGuids)
        {
            var applications = await applicationRepository.GetListByIdsAsync(applicationGuids);
            return await FilterBulkApplications([.. applications]);
        }

        public async Task<List<ApplicationApprovalDto>> FilterBulkApplications(Application[] applications)
        {
            var approvals = new List<ApplicationApprovalDto>();

            foreach (var application in applications)
            {
                if (await ValidateStateChange(application, GrantApplicationAction.Approve))
                {
                    approvals.Add(new ApplicationApprovalDto()
                    {
                        ApplicationId = application.Id,
                        ApplicationStatusId = application.ApplicationStatusId,
                        ApprovedAmount = application.ApprovedAmount,
                        RequestedAmount = application.RequestedAmount,
                        DecisionDate = application.FinalDecisionDate
                    });
                }

            }

            return approvals;
        }

        /// <summary>
        /// Validate the state of the application before attempting to change it (TODO: integrate into workflow / statemachine)
        /// </summary>
        /// <param name="application"></param>
        /// <param name="triggerAction"></param>
        /// <returns></returns>
        private async Task<bool> ValidateStateChange(Application application, GrantApplicationAction triggerAction)
        {
            if (MeetsWorkflowRequirement(application, triggerAction))
            {
                Logger.LogWarning("Approval requested for application in invalid state for approval: {ApplicationId}", application.Id);
                return false;
            }

            if (!await AuthorizationService.IsGrantedAsync(application, GetActionAuthorizationRequirement(triggerAction)))
            {
                Logger.LogWarning("Approval requested for application with insufficient permissions: {ApplicationId}", application.Id);
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
        private static bool MeetsWorkflowRequirement(Application application, GrantApplicationAction triggerAction)
        {
            if (triggerAction != GrantApplicationAction.Approve)
            {
                return false;
            }

            if (application.ApplicationStatus.StatusCode == GrantApplicationState.ASSESSMENT_COMPLETED)
            {
                return false;
            }

            return true;
        }

        private static OperationAuthorizationRequirement GetActionAuthorizationRequirement(GrantApplicationAction triggerAction)
        {
            return new OperationAuthorizationRequirement { Name = $"{GrantApplicationPermissions.Applications.Default}.{triggerAction}" };
            // this should allow anyone for now, but needs to change to a specific permission
        }
    }
}
