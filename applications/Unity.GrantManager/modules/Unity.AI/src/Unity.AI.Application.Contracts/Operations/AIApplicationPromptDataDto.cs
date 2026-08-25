using System;

namespace Unity.AI.Operations;

public class AIApplicationPromptDataDto
{
    public Guid ApplicationId { get; set; }

    public Guid ApplicationFormId { get; set; }

    public string ProjectName { get; set; } = string.Empty;

    public string ReferenceNo { get; set; } = string.Empty;

    public decimal RequestedAmount { get; set; }

    public decimal TotalProjectBudget { get; set; }

    public string? EconomicRegion { get; set; }

    public string? City { get; set; }

    public DateTime SubmissionDate { get; set; }

    public string? ProjectSummary { get; set; }

    public DateTime? ProjectStartDate { get; set; }

    public DateTime? ProjectEndDate { get; set; }

    public string? Community { get; set; }
}
