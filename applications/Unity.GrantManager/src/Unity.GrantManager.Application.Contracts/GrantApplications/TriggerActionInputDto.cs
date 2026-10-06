using System;

namespace Unity.GrantManager.GrantApplications;

// Optional fields supplied alongside a TriggerAction call to satisfy workflow-required fields
public class TriggerActionInputDto
{
    public DateTime? FinalDecisionDate { get; set; }
    public string? DeclineRational { get; set; }
}
