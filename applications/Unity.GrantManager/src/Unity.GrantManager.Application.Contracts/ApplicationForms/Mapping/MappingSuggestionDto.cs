namespace Unity.GrantManager.ApplicationForms.Mapping;

public class MappingSuggestionDto
{
    public string SourceField { get; set; } = string.Empty;
    public string TargetField { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public decimal Confidence { get; set; }
}
