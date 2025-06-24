using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Unity.GrantManager.Assessments;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Identity;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Applications;

// NOTE: See https://learn.microsoft.com/en-us/ef/core/miscellaneous/nullable-reference-types#required-navigation-properties

public class Application : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid ApplicationFormId { get; set; }

    public virtual ApplicationForm ApplicationForm
    {
        set => _applicationForm = value;
        get => _applicationForm
            ?? throw new InvalidOperationException("Uninitialized property: " + nameof(Applicant));
    }

    private ApplicationForm? _applicationForm;

    public Guid ApplicantId { get; set; }

    public virtual Applicant Applicant
    {
        set => _applicant = value;
        get => _applicant
               ?? throw new InvalidOperationException("Uninitialized property: " + nameof(Applicant));
    }

    private Applicant? _applicant;

    public virtual ApplicantAgent? ApplicantAgent { get; set; }

    public Guid ApplicationStatusId { get; set; }

    public virtual ApplicationStatus ApplicationStatus
    {
        set => _applicationStatus = value;
        get => _applicationStatus
               ?? throw new InvalidOperationException("Uninitialized property: " + nameof(ApplicationStatus));
    }
    private ApplicationStatus? _applicationStatus;

    public virtual Collection<Assessment>? Assessments { get; set; }
    public virtual Collection<ApplicationTags>? ApplicationTags { get; set; }
    public virtual Collection<ApplicationAssignment>? ApplicationAssignments { get; set; }

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

    // This is the Project Level Electoral District, not the Applicant's Electoral District.
    public string? ElectoralDistrict { get; set; }

    public string? Place { get; set; }

    public string? RegionalDistrict { get; set; }

    public Guid? TenantId { get; set; }

    public Guid? OwnerId { get; set; }
    // Navigation Property - Application Status
    public virtual Person? Owner { get; set; }


    public string? SigningAuthorityFullName { get; set; }
    public string? SigningAuthorityTitle { get; set; }
    public string? SigningAuthorityEmail { get; set; }
    public string? SigningAuthorityBusinessPhone { get; set; }
    public string? SigningAuthorityCellPhone { get; set; }
    public string? ContractNumber { get; set; }
    public DateTime? ContractExecutionDate { get; set; }
    public string? RiskRanking { get; set; }

    public bool IsInFinalDecisionState()
    {
        return GrantApplicationStateGroups.FinalDecisionStates.Contains(ApplicationStatus.StatusCode);
    }

    public void UpdateAlwaysChangeableFields(string? notes, string? subStatus, string? likelihoodOfFunding, decimal? totalProjectBudget, DateTime? notificationDate, string? riskRanking)
    {
        Notes = notes;
        SubStatus = subStatus;
        LikelihoodOfFunding = likelihoodOfFunding;
        TotalProjectBudget = totalProjectBudget ?? 0;
        NotificationDate = notificationDate;
        RiskRanking = riskRanking;

        // Recalculate percentage when TotalProjectBudget changes
        UpdatePercentageTotalProjectBudget();
    }

    public void UpdateApprovalFieldsRequiringPostEditPermission(decimal? approvedAmount)
    {
        ApprovedAmount = approvedAmount ?? 0;
    }

    public void UpdateAssessmentResultFieldsRequiringPostEditPermission(decimal? requestedAmount, int? totalScore)
    {
        RequestedAmount = requestedAmount ?? 0;
        TotalScore = totalScore ?? 0;

        // Recalculate percentage when RequestedAmount changes
        UpdatePercentageTotalProjectBudget();
    }

    public void UpdateAssessmentResultStatus(string? assessmentResultStatus)
    {
        if (assessmentResultStatus != AssessmentResultStatus)
        {
            AssessmentResultDate = DateTime.UtcNow;
        }

        AssessmentResultStatus = assessmentResultStatus;
    }

    public void UpdateFieldsOnlyForPreFinalDecision(string? dueDiligenceStatus, decimal? recommendedAmount, string? declineRational)
    {
        DueDiligenceStatus = dueDiligenceStatus;
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

    public void ValidateMinAndChangeApprovedAmount(decimal approvedAmount)
    {
        if ((ApprovedAmount != approvedAmount) && approvedAmount <= 0m)
        {
            throw new BusinessException("Approved amount cannot be 0.");
        }
    }

    public void ValidateMinAndChangeRecommendedAmount(decimal recommendedAmount, bool? isDirectApproval)
    {
        if (isDirectApproval != true && (RecommendedAmount != recommendedAmount) && recommendedAmount <= 0m)
        {
            throw new BusinessException("Recommended amount cannot be 0.");
        }
    }

    /// <summary>
    /// Calculates and updates the PercentageTotalProjectBudget property based on
    /// RequestedAmount and TotalProjectBudget values.
    /// This method should be called whenever either of those properties change.
    /// </summary>
    public void UpdatePercentageTotalProjectBudget()
    {
        PercentageTotalProjectBudget
            = (this.TotalProjectBudget == 0)
            ? 0 : decimal.Multiply(decimal.Divide(this.RequestedAmount, this.TotalProjectBudget), 100).To<double>();
    }

    /// <summary>
    /// Sets the Electoral District for the application.
    /// </summary>
    /// <param name="electoralDistrict"></param>
    public void SetElectoralDistrict(string electoralDistrict)
    {
        ElectoralDistrict = electoralDistrict;
    }
}

