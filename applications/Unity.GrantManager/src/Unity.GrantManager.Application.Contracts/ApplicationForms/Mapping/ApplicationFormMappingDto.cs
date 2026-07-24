using System;
using System.Collections.Generic;

namespace Unity.GrantManager.ApplicationForms.Mapping;

public class ApplicationFormMappingDto
{
    public Guid ApplicationFormVersionId { get; set; }
    public List<FormMappingDto> CoreFieldMatches { get; set; } = [];
    public List<FormWorksheetDto> WorksheetMatches { get; set; } = [];
    public List<WorksheetCreationSuggestionDto> WorksheetCreationSuggestions { get; set; } = [];
    public List<MappingIssueDto> Issues { get; set; } = [];
}
