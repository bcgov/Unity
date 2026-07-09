using System;
using System.Collections.Generic;

namespace Unity.GrantManager.ApplicationForms.Mapping;

public class ApplicationFormMappingSuggestionDto
{
    public Guid ApplicationFormVersionId { get; set; }
    public List<MappingSuggestionDto> CoreFieldMatches { get; set; } = [];
    public List<WorksheetMappingSuggestionDto> WorksheetMatches { get; set; } = [];
    public List<WorksheetCreationSuggestionDto> WorksheetCreationSuggestions { get; set; } = [];
    public List<MappingIssueDto> Issues { get; set; } = [];
}
