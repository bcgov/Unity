using System;

namespace Unity.GrantManager.ApplicationForms.Mapping;

public class FormMappingSuggestionDto
{
    public Guid Id { get; set; }
    public string SourceField { get; set; } = string.Empty;
    public string TargetField { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
    public string ChangeType { get; set; } = "New";
    public string? PreviousTargetField { get; set; }
    public string? ConflictSourceField { get; set; }
}
