using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Workflow;
using Volo.Abp.Domain.Services;
using Volo.Abp.Uow;

namespace Unity.GrantManager.Applications;
public class ApplicationManager : DomainService, IApplicationManager
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IApplicationStatusRepository _applicationStatusRepository;
    private readonly IApplicationUserAssignmentRepository _applicationUserAssignmentRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public ApplicationManager(
        IApplicationRepository applicationRepository,
        IApplicationStatusRepository applicationStatus,
        IApplicationUserAssignmentRepository applicationUserAssignmentRepository,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _applicationRepository = applicationRepository;
        _applicationStatusRepository = applicationStatus;
        _applicationUserAssignmentRepository = applicationUserAssignmentRepository;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public static void ConfigureWorkflow(StateMachine<GrantApplicationState, GrantApplicationAction> stateMachine)
    {
        // TODO: ENSURE APPLICATION STATE MACHINE MATCHES WORKFLOW IN AB#8375
        stateMachine.Configure(GrantApplicationState.OPEN)
            .InitialTransition(GrantApplicationState.SUBMITTED)
            .Permit(GrantApplicationAction.Withdraw, GrantApplicationState.WITHDRAWN)                             // 2.2 - Withdraw;          Role: Reviewer
            .Permit(GrantApplicationAction.Close, GrantApplicationState.CLOSED)                                   // 2.4 - Close Application; Role: Reviewer
            .Permit(GrantApplicationAction.Internal_Unasign, GrantApplicationState.SUBMITTED);

        stateMachine.Configure(GrantApplicationState.CLOSED);

        stateMachine.Configure(GrantApplicationState.SUBMITTED)
            .SubstateOf(GrantApplicationState.OPEN)
            .Permit(GrantApplicationAction.Internal_Assign, GrantApplicationState.ASSIGNED);                      // 2.1 - Internal_Assign;            Role: Team Lead

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
                IsPermitted = permittedActions.Contains(trigger),
                IsInternal = trigger.ToString().StartsWith("Internal_")
            })
            .OrderBy(x => (int)x.ApplicationAction)
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

        if(triggerAction == GrantApplicationAction.StartAssessment)
        {
            application.AssessmentStartDate = DateTime.UtcNow;
        }

        if((triggerAction == GrantApplicationAction.Approve) || (triggerAction == GrantApplicationAction.Deny))
        {
            application.FinalDecisionDate = DateTime.UtcNow;
        }

        return await _applicationRepository.UpdateAsync(application);
    }

    public async Task AssignUserAsync(Guid applicationId, string oidcSub, string assigneeDisplayName)
    {
        using var uow = _unitOfWorkManager.Begin();
        var userAssignment = await _applicationUserAssignmentRepository.InsertAsync(
            new ApplicationUserAssignment
            {
                OidcSub = oidcSub,
                ApplicationId = applicationId,
                AssigneeDisplayName = assigneeDisplayName,
                AssignmentTime = DateTime.UtcNow
            });

        var application = await _applicationRepository.GetAsync(userAssignment.ApplicationId, true);

        // BUSINESS RULE: If an application is in the SUBMITTED state and has
        // a user assigned, move to the ASSIGNED state.

        if (application != null && application.ApplicationStatus.StatusCode == GrantApplicationState.SUBMITTED)
        {
            await TriggerAction(application.Id, GrantApplicationAction.Internal_Assign);
        }

        await uow.SaveChangesAsync();
    }

    public async Task RemoveAssigneeAsync(Guid applicationId, string oidcSub)
    {
        using var uow = _unitOfWorkManager.Begin();
        var application = await _applicationRepository.GetAsync(applicationId, true);
        IQueryable<ApplicationUserAssignment> queryableAssignment = _applicationUserAssignmentRepository.GetQueryableAsync().Result;
        List<ApplicationUserAssignment> assignments = queryableAssignment.Where(a => a.ApplicationId.Equals(applicationId)).Where(b => b.OidcSub.Equals(oidcSub)).ToList();
        var assignmentRemoved = false;

        // Only remove the assignee if they were already assigned
        if (application != null)
        {
            var assignment = assignments.FirstOrDefault();
            if (null != assignment)
            {
                await _applicationUserAssignmentRepository.DeleteAsync(assignment);
                assignmentRemoved = true;
            }
        }

        // BUSINESS RULE: IF an application has all of its assignees removed,
        // set the application status back to SUBMITTED.
        var hasAssignees = (assignments.Count - 1) > 0 && assignmentRemoved;

        if (!hasAssignees && application!.ApplicationStatus.StatusCode == GrantApplicationState.ASSIGNED)
        {
            await TriggerAction(applicationId, GrantApplicationAction.Internal_Unasign);
        }

        await uow.SaveChangesAsync();
    }

    public async Task SetAssigneesAsync(Guid applicationId, List<(string oidcSub, string displayName)> oidcSubs)
    {
        using var uow = _unitOfWorkManager.Begin();
        var application = await _applicationRepository.GetAsync(applicationId, true);
        var currentUserAssignments = (await _applicationUserAssignmentRepository
            .GetQueryableAsync())
            .Where(s => s.ApplicationId == applicationId)
            .ToList();

        var hadAssignments = currentUserAssignments.Count > 0;
        var hasAssignees = oidcSubs.Count > 0;

        var assignmentsToDelete = new List<Guid>();

        // Remove all that shouldnt be there
        foreach (var assignment in currentUserAssignments)
        {
            var (oidcSub, displayName) = oidcSubs.Find(s => s.oidcSub == assignment.OidcSub);

            if (oidcSub == null && displayName == null)
            {
                assignmentsToDelete.Add(assignment.Id);
            }
        }

        // Add who should and are missing
        foreach (var (oidcSub, displayName) in oidcSubs)
        {
            var currentAssignment = currentUserAssignments.Find(s => s.OidcSub == oidcSub);
            if (currentAssignment == null)
            {
                await _applicationUserAssignmentRepository.InsertAsync(new ApplicationUserAssignment()
                {
                    ApplicationId = applicationId,
                    AssigneeDisplayName = displayName,
                    AssignmentTime = DateTime.UtcNow,
                    OidcSub = oidcSub
                }, false);
            }
        }

        await _applicationUserAssignmentRepository.DeleteManyAsync(assignmentsToDelete);

        // BUSINESS RULE: If an application is in the SUBMITTED state and has
        // a user assigned, move to the ASSIGNED state.

        // BUSINESS RULE: IF an application has all of its assignees removed,
        // set the application status back to SUBMITTED.

        if (hasAssignees && !hadAssignments && application.ApplicationStatus.StatusCode == GrantApplicationState.SUBMITTED) // If we now have assignees but didn't initially trigger state change
        {
            await TriggerAction(applicationId, GrantApplicationAction.Internal_Assign);
        }

        if (!hasAssignees && hadAssignments && application.ApplicationStatus.StatusCode == GrantApplicationState.ASSIGNED) // If we now have no assignees but started with assignees trigger state change
        {
            await TriggerAction(applicationId, GrantApplicationAction.Internal_Unasign);
        }
        await uow.SaveChangesAsync();
    }
}
