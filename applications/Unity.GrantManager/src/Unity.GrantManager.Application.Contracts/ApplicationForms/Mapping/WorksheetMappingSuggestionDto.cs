using System.Collections.Generic;

namespace Unity.GrantManager.ApplicationForms.Mapping;

public class WorksheetMappingSuggestionDto
{
    public string WorksheetName { get; set; } = string.Empty;
    public List<MappingSuggestionDto> FieldMatches { get; set; } = [];
}
