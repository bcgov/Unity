using System.Collections.Generic;

namespace Unity.GrantManager.ApplicationForms.Mapping;

public class WorksheetCreationSuggestionDto
{
    public string WorksheetName { get; set; } = string.Empty;
    public List<MappingFieldDto> SuggestedFields { get; set; } = [];
    public string Reason { get; set; } = string.Empty;
}
