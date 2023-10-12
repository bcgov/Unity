using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Workflow;
using Volo.Abp.Domain.Services;

namespace Unity.GrantManager.Applications;
public class ApplicationManager : DomainService, IApplicationManager
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IApplicationStatusRepository _applicationStatusRepository;

    public ApplicationManager(
        IApplicationRepository applicationRepository,
        IApplicationStatusRepository applicationStatus)
    {
        _applicationRepository = applicationRepository;
        _applicationStatusRepository = applicationStatus;
    }

    public static void ConfigureWorkflow(StateMachine<GrantApplicationState, GrantApplicationAction> stateMachine)
    {
        // TODO: ENSURE APPLICATION STATE MACHINE MATCHES WORKFLOW IN AB#8375
        stateMachine.Configure(GrantApplicationState.OPEN)
            .InitialTransition(GrantApplicationState.SUBMITTED)
            .Permit(GrantApplicationAction.Withdraw, GrantApplicationState.WITHDRAWN)                             // 2.2 - Withdraw;          Role: Reviewer
            .Permit(GrantApplicationAction.Close, GrantApplicationState.CLOSED);                                  // 2.4 - Close Application; Role: Reviewer

        stateMachine.Configure(GrantApplicationState.CLOSED);

        stateMachine.Configure(GrantApplicationState.SUBMITTED)
            .SubstateOf(GrantApplicationState.OPEN)
            .Permit(GrantApplicationAction.Assign, GrantApplicationState.ASSIGNED);                               // 2.1 - Assign;            Role: Team Lead

        stateMachine.Configure(GrantApplicationState.ASSIGNED)
            .SubstateOf(GrantApplicationState.OPEN)
            .Permit(GrantApplicationAction.StartReview, GrantApplicationState.UNDER_INITIAL_REVIEW);              // 2.3 - Start Review;      Role: Reviewer 

        stateMachine.Configure(GrantApplicationState.UNDER_INITIAL_REVIEW)
            .SubstateOf(GrantApplicationState.OPEN)
            .Permit(GrantApplicationAction.CompleteReview, GrantApplicationState.INITITAL_REVIEW_COMPLETED);

        stateMachine.Configure(GrantApplicationState.INITITAL_REVIEW_COMPLETED)
            .SubstateOf(GrantApplicationState.OPEN)
            .Permit(GrantApplicationAction.StartAssessment, GrantApplicationState.UNDER_ASSESSMENT);

        stateMachine.Configure(GrantApplicationState.UNDER_ASSESSMENT)
            .SubstateOf(GrantApplicationState.OPEN)
            .Permit(GrantApplicationAction.CompleteAssessment, GrantApplicationState.ASSESSMENT_COMPLETED);

        stateMachine.Configure(GrantApplicationState.ASSESSMENT_COMPLETED)
            .SubstateOf(GrantApplicationState.OPEN)
            .Permit(GrantApplicationAction.Approve, GrantApplicationState.GRANT_APPROVED)
            .Permit(GrantApplicationAction.Deny, GrantApplicationState.GRANT_NOT_APPROVED);

        // CLOSED STATES
        stateMachine.Configure(GrantApplicationState.CLOSED);

        stateMachine.Configure(GrantApplicationState.WITHDRAWN);

        stateMachine.Configure(GrantApplicationState.GRANT_APPROVED);

        stateMachine.Configure(GrantApplicationState.GRANT_NOT_APPROVED);
    }

    public async Task<List<ApplicationActionResultItem>> GetActions(Guid applicationId)
    {
        var application = await _applicationRepository.GetAsync(applicationId, true);

        // NOTE: Should be mapped to ApplicationStatus ID through enum value instead of nav property
        var Workflow = new UnityWorkflow<GrantApplicationState, GrantApplicationAction>(
            () => application.ApplicationStatus.StatusCode,
            s => application.ApplicationStatus.StatusCode = s,
        ConfigureWorkflow);

        var allActions = Workflow.GetAllActions().ToList();
        var permittedActions = Workflow.GetPermittedActions().ToList();

        var actionsList = allActions
            .Select(trigger =>
            new ApplicationActionResultItem
            {
                ApplicationAction = trigger,
                IsPermitted = permittedActions.Contains(trigger)
            })
            .OrderBy(x => (int) x.ApplicationAction)
            .ToList();

        return actionsList;
    }

    public async Task<Application> TriggerAction(Guid applicationId, GrantApplicationAction triggerAction)
    {
        var application = await _applicationRepository.GetAsync(applicationId);
        var statusChange = application.ApplicationStatus.StatusCode;

        // NOTE: Should be mapped to ApplicationStatus ID through enum value instead of nav property
        // WARNING: DRAFT CODE - MAY NOT BE PERSISTING STATE TRANSITIONS CORRECTLY
        var Workflow = new UnityWorkflow<GrantApplicationState, GrantApplicationAction>(
            () => statusChange,
            s => statusChange = s,
        ConfigureWorkflow);

        await Workflow.ExecuteActionAsync(triggerAction);

        var statusChangedTo = await _applicationStatusRepository.GetAsync(x => x.StatusCode.Equals(statusChange));
        
        // NOTE: Is this required or can the navigation property be set on its own?
        application.ApplicationStatusId = statusChangedTo.Id;
        application.ApplicationStatus = statusChangedTo;

        return await _applicationRepository.UpdateAsync(application);
    }
}
