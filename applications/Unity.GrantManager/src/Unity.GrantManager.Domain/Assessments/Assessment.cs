using Stateless;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Workflow;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Assessments;

public class Assessment : AuditedAggregateRoot<Guid>, IHasWorkflow<AssessmentState, AssessmentAction>, IMultiTenant
{
    public Guid ApplicationId { get; private set; }
    public virtual Application Application
    {
        set => _application = value;
        get => _application
               ?? throw new InvalidOperationException("Uninitialized property: " + nameof(Application));
    }
    private Application? _application;

    public Guid AssessorId { get; private set; }

    public DateTime? EndDate { get; private set; }

    public bool IsComplete { get; private set; }

    public bool? ApprovalRecommended { get; set; }

    public AssessmentState Status { get; private set; }
        
    public int? FinancialAnalysis { get; set; }
    public int? EconomicImpact { get; set; }
    public int? InclusiveGrowth { get; set; }
    public int? CleanGrowth { get; set; }


    [NotMapped]
    public UnityWorkflow<AssessmentState, AssessmentAction> Workflow { get; private set; }

    public Guid? TenantId { get; set; }

    public Assessment()
    {
        /* This constructor is for deserialization / ORM purpose */
        Workflow = new UnityWorkflow<AssessmentState, AssessmentAction>(() => Status, s => Status = s, ConfigureWorkflow);
    }

    public Assessment(
        Guid id,
        Guid applicationId,
        Guid assessorId,
        AssessmentState status = AssessmentState.IN_PROGRESS)
        : base(id)
    {
        ApplicationId = applicationId;
        AssessorId = assessorId;
        Status = status;
        Workflow = new UnityWorkflow<AssessmentState, AssessmentAction>(() => Status, s => Status = s, ConfigureWorkflow);
    }

    public void ConfigureWorkflow(StateMachine<AssessmentState, AssessmentAction> stateMachine)
    {
        stateMachine.Configure(AssessmentState.IN_PROGRESS)
            .Permit(AssessmentAction.Complete, AssessmentState.COMPLETED);

        // NOTE: This will be removed when there are no more assessments IN_REVIEW
        stateMachine.Configure(AssessmentState.IN_REVIEW)
            .Permit(AssessmentAction.SendBack, AssessmentState.IN_PROGRESS)
            .Permit(AssessmentAction.Complete, AssessmentState.COMPLETED);

        stateMachine.Configure(AssessmentState.COMPLETED)
            .Permit(AssessmentAction.SendBack, AssessmentState.IN_PROGRESS)
            .OnEntry(OnResolved)
            .OnExit(OnReopened);
    }

    private void OnResolved()
    {
        EndDate = DateTime.UtcNow;
        IsComplete = true;
    }

    private void OnReopened()
    {
        EndDate = null;
    }
}

