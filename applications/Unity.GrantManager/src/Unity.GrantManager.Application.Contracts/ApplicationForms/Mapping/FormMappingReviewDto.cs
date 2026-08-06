using System;
using System.Collections.Generic;

namespace Unity.GrantManager.ApplicationForms.Mapping;

public class FormMappingReviewDto
{
    public Guid FormVersionId { get; set; }
    public int Sequence { get; set; }
    public GenerationReviewStatus Status { get; set; }
    public FormMappingReviewPhase Phase { get; set; }
    public FormGenerationWorkflowState WorkflowState { get; set; }
    public FormGenerationWorkflowAction WorkflowAction { get; set; }
    public string State { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public List<FormGenerationWorkflowAction> AvailableActions { get; set; } = [];
    public bool ActionEnabled { get; set; }
    public string StateLabel { get; set; } = string.Empty;
    public string ActionLabel { get; set; } = string.Empty;
    public List<FormMappingSuggestionDto> PendingSuggestions { get; set; } = [];
    public List<string> AcceptedWorksheetFields { get; set; } = [];
    public List<Guid> DraftWorksheetIds { get; set; } = [];
    public bool CanGenerateFinalMapping { get; set; }
}
