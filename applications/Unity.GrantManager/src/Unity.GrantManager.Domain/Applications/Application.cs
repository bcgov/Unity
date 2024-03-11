using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Unity.GrantManager.GrantApplications;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Applications;

public class Application : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid ApplicationFormId { get; set; }
    public Guid ApplicantId { get; set; }
    public Guid ApplicationStatusId { get; set; }

    // Navigation Property - Application Status
    public virtual ApplicationStatus ApplicationStatus
    {
        // NOTE: See https://learn.microsoft.com/en-us/ef/core/miscellaneous/nullable-reference-types#required-navigation-properties
        set => _applicationStatus = value;
        get => _applicationStatus
               ?? throw new InvalidOperationException("Uninitialized property: " + nameof(ApplicationStatus));
    }
    private ApplicationStatus? _applicationStatus;

    public string ProjectName { get; set; } = string.Empty;
    public string ReferenceNo { get; set; } = string.Empty;
    public decimal RequestedAmount { get; set; }
    public decimal TotalProjectBudget { get; set; }
    public string? EconomicRegion { get; set; } = null;
    public string? City { get; set; } = null;
    public DateTime? ProposalDate { get; set; }
    public DateTime SubmissionDate { get; set; }
    public DateTime? AssessmentStartDate { get; set; }
    public DateTime? FinalDecisionDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? NotificationDate { get; set; }

    [Column(TypeName = "jsonb")]
    public string? Payload { get; set; }

    public string? ProjectSummary { get; set; }

    public int? TotalScore { get; set; } = null;

    public decimal RecommendedAmount { get; set; } = 0;

    public decimal ApprovedAmount { get; set; } = 0;

    public string? LikelihoodOfFunding { get; set; }

    public string? DueDiligenceStatus { get; set; }

    public string? SubStatus { get; set; }

    public string? DeclineRational { get; set; }

    public string? Notes { get; set; }

    public string? AssessmentResultStatus { get; set; }

    public DateTime? AssessmentResultDate { get; set; }

    public DateTime? ProjectStartDate { get; set; }

    public DateTime? ProjectEndDate { get; set; }

    public double? PercentageTotalProjectBudget { get; set; }

    public decimal? ProjectFundingTotal { get; set; }

    public string? Community { get; set; }

    public int? CommunityPopulation { get; set; }

    public string? Acquisition { get; set; }

    public string? Forestry { get; set; }

    public string? ForestryFocus { get; set; }

    public string? ElectoralDistrict { get; set; }

    public string? RegionalDistrict { get; set; }

    public Guid? TenantId { get; set; }

    public Guid? OwnerId { get; set; }

    public string? SigningAuthorityFullName { get; set; }
    public string? SigningAuthorityTitle { get; set; }
    public string? SigningAuthorityEmail { get; set; }
    public string? SigningAuthorityBusinessPhone { get; set; }
    public string? SigningAuthorityCellPhone { get; set; }

    public bool IsInFinalDecisionState()
    {
        return GrantApplicationStateGroups.FinalDecisionStates.Contains(ApplicationStatus.StatusCode);
    }

    public void UpdateAlwaysChangeableFields(string? notes, string? subStatus, string? likelihoodOfFunding)
    {
        Notes = notes;
        SubStatus = subStatus;
        LikelihoodOfFunding = likelihoodOfFunding;
    }

    public void UpdateFieldsRequiringPostEditPermission(decimal? approvedAmount, decimal? requestedAmount, int? totalScore)
    {
        ApprovedAmount = approvedAmount ?? 0;
        RequestedAmount = requestedAmount ?? 0;
        TotalScore = totalScore ?? 0;
    }

    public void UpdateAssessmentResultStatus(string? assessmentResultStatus)
    {
        if (assessmentResultStatus != AssessmentResultStatus)
        {
            AssessmentResultDate = DateTime.UtcNow;
        }

        AssessmentResultStatus = assessmentResultStatus;
    }

    public void UpdateFieldsOnlyForPreFinalDecision(string? projectSummary, string? dueDiligenceStatus, decimal? totalProjectBudget, decimal? recommendedAmount, string? declineRational)
    {
        ProjectSummary = projectSummary;
        DueDiligenceStatus = dueDiligenceStatus;
        TotalProjectBudget = totalProjectBudget ?? 0;
        RecommendedAmount = recommendedAmount ?? 0;
        DeclineRational = declineRational;
    }

    public void ValidateAndChangeDueDate(DateTime? dueDate)
    {
        if ((DueDate != dueDate) && dueDate != null && dueDate.Value < DateTime.Now.AddDays(-1))
        {
            throw new BusinessException("Due Date cannot be a past date.");
        }
        else
        {
            DueDate = dueDate;
        }
    }

    public void ValidateAndChangeNotificationDate(DateTime? notificationDate)
    {
        if ((NotificationDate != notificationDate) && notificationDate != null && notificationDate.Value < DateTime.Now.AddDays(-1))
        {
            throw new BusinessException("Notification Date cannot be a past date.");
        }
        else
        {
            NotificationDate = notificationDate;
        }
    }

    public void ValidateAndChangeFinalDecisionDate(DateTime? finalDecisionDate)
    {
        if ((FinalDecisionDate != finalDecisionDate) && finalDecisionDate != null && finalDecisionDate.Value > DateTime.Now)
        {
            throw new BusinessException("Decision Date cannot not be a future date.");
        }
        else
        {
            FinalDecisionDate = finalDecisionDate;
        }
    }
}
