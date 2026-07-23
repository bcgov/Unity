using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Workflow;
using Volo.Abp.Domain.Services;

namespace Unity.GrantManager.Applications;

public class OnboardingApplicationManager(
    IApplicationRepository applicationRepository,
    IApplicationStatusRepository applicationStatusRepository)
    : DomainService
{
    private static void ConfigureWorkflow(StateMachine<GrantApplicationState, GrantApplicationAction> sm)
    {
        sm.Configure(GrantApplicationState.SUBMITTED)
            .Permit(GrantApplicationAction.Approve, GrantApplicationState.GRANT_APPROVED)
            .Permit(GrantApplicationAction.Deny, GrantApplicationState.GRANT_NOT_APPROVED);

        sm.Configure(GrantApplicationState.GRANT_APPROVED)
            .Permit(GrantApplicationAction.Close, GrantApplicationState.CLOSED);

        sm.Configure(GrantApplicationState.GRANT_NOT_APPROVED)
            .Permit(GrantApplicationAction.Close, GrantApplicationState.CLOSED);

        sm.Configure(GrantApplicationState.CLOSED);
    }

    public async Task<List<ApplicationActionResultItem>> GetActions(Guid applicationId)
    {
        var application = await applicationRepository.GetAsync(applicationId, includeDetails: true);
        var statusCode = application.ApplicationStatus.StatusCode;

        var workflow = new UnityWorkflow<GrantApplicationState, GrantApplicationAction>(
            () => statusCode,
            s => statusCode = s,
            ConfigureWorkflow);

        var allActions = workflow.GetAllActions().Distinct().ToList();
        var permittedActions = (await workflow.GetPermittedActions()).ToList();

        return allActions
            .Select(trigger => new ApplicationActionResultItem
            {
                ApplicationAction = trigger,
                IsPermitted = permittedActions.Contains(trigger),
                IsInternal = false
            })
            .OrderBy(x => (int)x.ApplicationAction)
            .ToList();
    }

    public static async Task<bool> IsActionAllowed(Application application, GrantApplicationAction triggerAction)
    {
        var statusCode = application.ApplicationStatus.StatusCode;
        var workflow = new UnityWorkflow<GrantApplicationState, GrantApplicationAction>(
            () => statusCode,
            s => statusCode = s,
            ConfigureWorkflow);

        return (await workflow.GetPermittedActions()).Contains(triggerAction);
    }

    public async Task<Application> TriggerAction(Guid applicationId, GrantApplicationAction triggerAction)
    {
        var application = await applicationRepository.GetAsync(applicationId, includeDetails: true);
        var statusCode = application.ApplicationStatus.StatusCode;

        var workflow = new UnityWorkflow<GrantApplicationState, GrantApplicationAction>(
            () => statusCode,
            s => statusCode = s,
            ConfigureWorkflow);

        await workflow.ExecuteActionAsync(triggerAction);

        var newStatus = await applicationStatusRepository.GetAsync(x => x.StatusCode.Equals(statusCode));
        application.ApplicationStatusId = newStatus.Id;
        application.ApplicationStatus = newStatus;
        application.LastModificationTime = DateTime.UtcNow;

        if (triggerAction == GrantApplicationAction.Approve || triggerAction == GrantApplicationAction.Deny)
        {
            application.FinalDecisionDate = DateTime.UtcNow;
        }

        return await applicationRepository.UpdateAsync(application);
    }
}
