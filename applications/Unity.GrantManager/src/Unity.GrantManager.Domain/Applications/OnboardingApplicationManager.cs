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

namespace Unity.GrantManager.Applications;

public class OnboardingApplicationManager(
    IApplicationRepository applicationRepository,
    IApplicationStatusRepository applicationStatusRepository,
    IApplicationAssignmentRepository applicationAssignmentRepository,
    IPersonRepository personRepository)
    : DomainService
{
    private static void ConfigureWorkflow(StateMachine<GrantApplicationState, GrantApplicationAction> sm)
    {
        sm.Configure(GrantApplicationState.SUBMITTED)
            .Permit(GrantApplicationAction.Approve, GrantApplicationState.GRANT_APPROVED)
            .Permit(GrantApplicationAction.Deny, GrantApplicationState.GRANT_NOT_APPROVED)
            .Permit(GrantApplicationAction.Defer, GrantApplicationState.DEFER);

        sm.Configure(GrantApplicationState.GRANT_APPROVED)
            .Permit(GrantApplicationAction.Close, GrantApplicationState.CLOSED)
            .Permit(GrantApplicationAction.Defer, GrantApplicationState.DEFER);

        sm.Configure(GrantApplicationState.GRANT_NOT_APPROVED)
            .Permit(GrantApplicationAction.Close, GrantApplicationState.CLOSED)
            .Permit(GrantApplicationAction.Defer, GrantApplicationState.DEFER);

        sm.Configure(GrantApplicationState.CLOSED)
            .Permit(GrantApplicationAction.Submit, GrantApplicationState.SUBMITTED);

        // A deferred request re-enters through Submitted (and needs a fresh decision), or is closed.
        sm.Configure(GrantApplicationState.DEFER)
            .Permit(GrantApplicationAction.Submit, GrantApplicationState.SUBMITTED)
            .Permit(GrantApplicationAction.Close, GrantApplicationState.CLOSED);
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
        var permittedActions = (await workflow.GetPermittedActionsAsync()).ToList();

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

        return (await workflow.GetPermittedActionsAsync()).Contains(triggerAction);
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

    /// <summary>
    /// Makes every given application carry exactly this owner and these assignees, replacing what
    /// was there. Deliberately does not use ApplicationManager's assignment methods: they move
    /// SUBMITTED to ASSIGNED, a state the onboarding workflow does not have, so status is left alone.
    /// </summary>
    public async Task SetAssignmentAsync(
        IReadOnlyCollection<Guid> applicationIds,
        Guid? ownerId,
        IReadOnlyCollection<(Guid AssigneeId, string? Role)> assignees)
    {
        var assigneeIds = assignees.Select(a => a.AssigneeId).ToList();
        if (assigneeIds.Distinct().Count() != assigneeIds.Count)
        {
            throw new BusinessException("GrantManager:OnboardingDuplicateAssignee");
        }

        await EnsurePersonsExistAsync(assigneeIds, ownerId);

        var ids = applicationIds.Distinct().ToList();
        var existingAssignments = await applicationAssignmentRepository.GetListAsync(a => ids.Contains(a.ApplicationId));

        foreach (var applicationId in ids)
        {
            var application = await applicationRepository.GetAsync(applicationId, includeDetails: false);
            application.OwnerId = ownerId;
            await applicationRepository.UpdateAsync(application);

            await SyncAssigneesAsync(
                applicationId,
                existingAssignments.Where(a => a.ApplicationId == applicationId).ToList(),
                assignees);
        }
    }

    private async Task EnsurePersonsExistAsync(List<Guid> assigneeIds, Guid? ownerId)
    {
        var personIds = assigneeIds.ToHashSet();
        if (ownerId.HasValue)
        {
            personIds.Add(ownerId.Value);
        }

        if (personIds.Count == 0)
        {
            return;
        }

        var found = await personRepository.GetListAsync(p => personIds.Contains(p.Id));
        if (found.Count != personIds.Count)
        {
            throw new BusinessException("GrantManager:OnboardingAssigneeNotFound");
        }
    }

    private async Task SyncAssigneesAsync(
        Guid applicationId,
        List<ApplicationAssignment> current,
        IReadOnlyCollection<(Guid AssigneeId, string? Role)> assignees)
    {
        foreach (var assignment in current.Where(c => !assignees.Any(a => a.AssigneeId == c.AssigneeId)))
        {
            await applicationAssignmentRepository.DeleteAsync(assignment);
        }

        foreach (var (assigneeId, role) in assignees)
        {
            var duty = string.IsNullOrWhiteSpace(role) ? null : role.Trim();
            var match = current.Find(c => c.AssigneeId == assigneeId);

            if (match == null)
            {
                await applicationAssignmentRepository.InsertAsync(new ApplicationAssignment
                {
                    ApplicationId = applicationId,
                    AssigneeId = assigneeId,
                    Duty = duty
                });
            }
            else if (match.Duty != duty)
            {
                match.Duty = duty;
                await applicationAssignmentRepository.UpdateAsync(match);
            }
        }
    }
}
