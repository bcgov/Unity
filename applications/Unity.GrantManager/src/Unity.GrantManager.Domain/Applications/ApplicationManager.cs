using Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Identity;
using Unity.GrantManager.Workflow;
using Volo.Abp;
using Volo.Abp.Domain.Services;
using Volo.Abp.Uow;

namespace Unity.GrantManager.Applications;
public class ApplicationManager : DomainService, IApplicationManager
{
    private readonly IApplicationRepository _applicationRepository;
    private readonly IApplicationStatusRepository _applicationStatusRepository;
    private readonly IApplicationAssignmentRepository _applicationAssignmentRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly IPersonRepository _personRepository;

    public ApplicationManager(
        IApplicationRepository applicationRepository,
        IApplicationStatusRepository applicationStatus,
        IApplicationAssignmentRepository applicationAssignmentRepository,
        IUnitOfWorkManager unitOfWorkManager,
        IPersonRepository personRepository)
    {
        _applicationRepository = applicationRepository;
        _applicationStatusRepository = applicationStatus;
        _applicationAssignmentRepository = applicationAssignmentRepository;
        _unitOfWorkManager = unitOfWorkManager;
        _personRepository = personRepository;
    }

    public static void ConfigureWorkflow(StateMachine<GrantApplicationState, GrantApplicationAction> stateMachine)
    {
        // TODO: ENSURE APPLICATION STATE MACHINE MATCHES WORKFLOW IN AB#8375
        stateMachine.Configure(GrantApplicationState.OPEN)
            .InitialTransition(GrantApplicationState.SUBMITTED)
            .Permit(GrantApplicationAction.Withdraw, GrantApplicationState.WITHDRAWN)                             // 2.2 - Withdraw;          Role: Reviewer
            .Permit(GrantApplicationAction.Close, GrantApplicationState.CLOSED)                                   // 2.4 - Close Application; Role: Reviewer
            .Permit(GrantApplicationAction.Internal_Unasign, GrantApplicationState.SUBMITTED);

        stateMachine.Configure(GrantApplicationState.SUBMITTED)
            .SubstateOf(GrantApplicationState.OPEN)
            .Permit(GrantApplicationAction.Defer, GrantApplicationState.DEFER)
            .Permit(GrantApplicationAction.OnHold, GrantApplicationState.ON_HOLD)
            .Permit(GrantApplicationAction.Internal_Assign, GrantApplicationState.ASSIGNED);                      // 2.1 - Internal_Assign;            Role: Team Lead

        stateMachine.Configure(GrantApplicationState.ASSIGNED)
            .SubstateOf(GrantApplicationState.OPEN)
            .Permit(GrantApplicationAction.Defer, GrantApplicationState.DEFER)
            .Permit(GrantApplicationAction.OnHold, GrantApplicationState.ON_HOLD)
            .Permit(GrantApplicationAction.StartReview, GrantApplicationState.UNDER_INITIAL_REVIEW);              // 2.3 - Start Review;      Role: Reviewer 

        stateMachine.Configure(GrantApplicationState.UNDER_INITIAL_REVIEW)
            .SubstateOf(GrantApplicationState.OPEN)
            .Permit(GrantApplicationAction.Defer, GrantApplicationState.DEFER)
            .Permit(GrantApplicationAction.OnHold, GrantApplicationState.ON_HOLD)
            .Permit(GrantApplicationAction.CompleteReview, GrantApplicationState.INITITAL_REVIEW_COMPLETED);

        stateMachine.Configure(GrantApplicationState.INITITAL_REVIEW_COMPLETED)
            .SubstateOf(GrantApplicationState.OPEN)
            .Permit(GrantApplicationAction.Defer, GrantApplicationState.DEFER)
            .Permit(GrantApplicationAction.OnHold, GrantApplicationState.ON_HOLD)
            .Permit(GrantApplicationAction.StartAssessment, GrantApplicationState.UNDER_ASSESSMENT);

        stateMachine.Configure(GrantApplicationState.UNDER_ASSESSMENT)
            .SubstateOf(GrantApplicationState.OPEN)
            .Permit(GrantApplicationAction.Defer, GrantApplicationState.DEFER)
            .Permit(GrantApplicationAction.OnHold, GrantApplicationState.ON_HOLD)
            .Permit(GrantApplicationAction.CompleteAssessment, GrantApplicationState.ASSESSMENT_COMPLETED);

        stateMachine.Configure(GrantApplicationState.ASSESSMENT_COMPLETED)
            .SubstateOf(GrantApplicationState.OPEN)
            .Permit(GrantApplicationAction.Defer, GrantApplicationState.DEFER)
            .Permit(GrantApplicationAction.OnHold, GrantApplicationState.ON_HOLD)
            .Permit(GrantApplicationAction.Approve, GrantApplicationState.GRANT_APPROVED)
            .Permit(GrantApplicationAction.Deny, GrantApplicationState.GRANT_NOT_APPROVED);
        
        stateMachine.Configure(GrantApplicationState.DEFER)
            .SubstateOf(GrantApplicationState.OPEN)
            .Permit(GrantApplicationAction.Close, GrantApplicationState.CLOSED)
            .Permit(GrantApplicationAction.StartReview, GrantApplicationState.UNDER_INITIAL_REVIEW)
            .Permit(GrantApplicationAction.CompleteReview, GrantApplicationState.INITITAL_REVIEW_COMPLETED)
            .Permit(GrantApplicationAction.StartAssessment, GrantApplicationState.UNDER_ASSESSMENT)
            .Permit(GrantApplicationAction.CompleteAssessment, GrantApplicationState.ASSESSMENT_COMPLETED)
            .Permit(GrantApplicationAction.OnHold, GrantApplicationState.ON_HOLD);

        stateMachine.Configure(GrantApplicationState.ON_HOLD)
            .SubstateOf(GrantApplicationState.OPEN)
            .Permit(GrantApplicationAction.Close, GrantApplicationState.CLOSED)
            .Permit(GrantApplicationAction.StartReview, GrantApplicationState.UNDER_INITIAL_REVIEW)
            .Permit(GrantApplicationAction.CompleteReview, GrantApplicationState.INITITAL_REVIEW_COMPLETED)
            .Permit(GrantApplicationAction.StartAssessment, GrantApplicationState.UNDER_ASSESSMENT)
            .Permit(GrantApplicationAction.CompleteAssessment, GrantApplicationState.ASSESSMENT_COMPLETED)
            .Permit(GrantApplicationAction.Defer, GrantApplicationState.DEFER);

        // CLOSED STATES

        stateMachine.Configure(GrantApplicationState.CLOSED)
            .Permit(GrantApplicationAction.Withdraw, GrantApplicationState.WITHDRAWN)
            .Permit(GrantApplicationAction.Defer, GrantApplicationState.DEFER)
            .Permit(GrantApplicationAction.OnHold, GrantApplicationState.ON_HOLD);


        stateMachine.Configure(GrantApplicationState.WITHDRAWN)
            .Permit(GrantApplicationAction.Close, GrantApplicationState.CLOSED)
            .Permit(GrantApplicationAction.Defer, GrantApplicationState.DEFER)
            .Permit(GrantApplicationAction.OnHold, GrantApplicationState.ON_HOLD);

        stateMachine.Configure(GrantApplicationState.GRANT_APPROVED)
            .Permit(GrantApplicationAction.Withdraw, GrantApplicationState.WITHDRAWN)
            .Permit(GrantApplicationAction.Close, GrantApplicationState.CLOSED);

        stateMachine.Configure(GrantApplicationState.GRANT_NOT_APPROVED)
            .Permit(GrantApplicationAction.Close, GrantApplicationState.CLOSED);
    } 

    public async Task<List<ApplicationActionResultItem>> GetActions(Guid applicationId)
    {
        var application = await _applicationRepository.GetAsync(applicationId, true);

        // NOTE: Should be mapped to ApplicationStatus ID through enum value instead of nav property
        var Workflow = new UnityWorkflow<GrantApplicationState, GrantApplicationAction>(
            () => application.ApplicationStatus.StatusCode,
            s => application.ApplicationStatus.StatusCode = s, ConfigureWorkflow);

        var allActions = Workflow.GetAllActions().Distinct().ToList();
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

        if (triggerAction == GrantApplicationAction.Deny && application.DeclineRational.IsNullOrEmpty())
        {
            throw new UserFriendlyException("The \"Decline Rationale\" is Required for application denial");
        }

        if ((triggerAction == GrantApplicationAction.Approve || triggerAction == GrantApplicationAction.Deny) && application.FinalDecisionDate == null)
        {
            throw new UserFriendlyException("The Decision Date is Required.");
        }

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

        if (triggerAction == GrantApplicationAction.StartAssessment)
        {
            application.AssessmentStartDate = DateTime.UtcNow;
        }

        return await _applicationRepository.UpdateAsync(application);
    }

    public async Task AssignUserAsync(Guid applicationId, Guid assigneeId, string? duty)
    {
        using var uow = _unitOfWorkManager.Begin();
        var person = await _personRepository.FindAsync(assigneeId) ?? throw new BusinessException("Tenant User Missing!");
        var userAssignment = await _applicationAssignmentRepository.InsertAsync(
            new ApplicationAssignment
            {
                AssigneeId = person.Id,
                ApplicationId = applicationId,
                Duty = duty,
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
    public async Task UpdateAssigneeAsync(Guid applicationId, Guid assigneeId, string? duty)
    {
        using var uow = _unitOfWorkManager.Begin();
        var person = await _personRepository.FindAsync(assigneeId) ?? throw new BusinessException("Tenant User Missing!");

        var userAssignment = await _applicationAssignmentRepository.GetAsync(e => e.ApplicationId == applicationId && e.AssigneeId == assigneeId);

        if (userAssignment != null)
        {
            userAssignment.Duty = duty;

            await _applicationAssignmentRepository.UpdateAsync(userAssignment);
        }
        else
        {
            await _applicationAssignmentRepository.InsertAsync(
             new ApplicationAssignment
             {
                 AssigneeId = person.Id,
                 ApplicationId = applicationId,
                 Duty = duty
             });
        }


        var application = await _applicationRepository.GetAsync(applicationId, true);

        // BUSINESS RULE: If an application is in the SUBMITTED state and has
        // a user assigned, move to the ASSIGNED state.

        if (application != null && application.ApplicationStatus.StatusCode == GrantApplicationState.SUBMITTED)
        {
            await TriggerAction(application.Id, GrantApplicationAction.Internal_Assign);
        }

        await uow.SaveChangesAsync();
    }

    public async Task RemoveAssigneeAsync(Guid applicationId, Guid assigneeId)
    {
        using var uow = _unitOfWorkManager.Begin();
        var person = await _personRepository.FindAsync(assigneeId) ?? throw new BusinessException("Tenant User Missing!");
        var application = await _applicationRepository.GetAsync(applicationId, true);
        IQueryable<ApplicationAssignment> queryableAssignment = _applicationAssignmentRepository.GetQueryableAsync().Result;
        List<ApplicationAssignment> assignments = queryableAssignment
            .Where(a => a.ApplicationId.Equals(applicationId))
            .Where(b => b.AssigneeId.Equals(person.Id)).ToList();

        var assignmentRemoved = false;

        // Only remove the assignee if they were already assigned
        if (application != null)
        {
            var assignment = assignments.FirstOrDefault();
            if (null != assignment)
            {
                await _applicationAssignmentRepository.DeleteAsync(assignment);
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

    public async Task SetAssigneesAsync(Guid applicationId, List<(Guid? assigneeId, string? fullName)> assigneeSubs)
    {
        using var uow = _unitOfWorkManager.Begin();
        var application = await _applicationRepository.GetAsync(applicationId, true);
        var currentUserAssignments = (await _applicationAssignmentRepository
            .GetQueryableAsync())
            .Where(s => s.ApplicationId == applicationId)
            .ToList();

        var hadAssignments = currentUserAssignments.Count > 0;
        var hasAssignees = assigneeSubs.Count > 0;

        var assignmentsToDelete = new List<Guid>();

        // Remove all that shouldnt be there
        foreach (var assignment in currentUserAssignments)
        {
            var (assigneeId, fullName) = assigneeSubs.Find(s => s.assigneeId == assignment.AssigneeId);

            if (assigneeId == null && fullName == null)
            {
                assignmentsToDelete.Add(assignment.Id);
            }
        }

        // Add who should and are missing
        foreach (var (assigneeId, fullName) in assigneeSubs)
        {
            var currentAssignment = currentUserAssignments.Find(s => s.AssigneeId == assigneeId);
            if (currentAssignment == null && assigneeId != null)
            {
                await _applicationAssignmentRepository.InsertAsync(new ApplicationAssignment()
                {
                    ApplicationId = applicationId,
                    AssigneeId = assigneeId.Value
                }, false);
            }
        }

        await _applicationAssignmentRepository.DeleteManyAsync(assignmentsToDelete);

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
