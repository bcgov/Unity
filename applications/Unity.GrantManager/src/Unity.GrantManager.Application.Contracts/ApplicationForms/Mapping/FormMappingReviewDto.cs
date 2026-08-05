using System;
using System.Collections.Generic;

namespace Unity.GrantManager.ApplicationForms.Mapping;

public class FormMappingReviewDto
{
    public Guid FormVersionId { get; set; }
    public FormMappingReviewPhase Phase { get; set; }
    public List<FormMappingSuggestionDto> PendingSuggestions { get; set; } = [];
    public List<string> AcceptedWorksheetFields { get; set; } = [];
    public List<Guid> DraftWorksheetIds { get; set; } = [];
    public bool CanGenerateFinalMapping { get; set; }
}
