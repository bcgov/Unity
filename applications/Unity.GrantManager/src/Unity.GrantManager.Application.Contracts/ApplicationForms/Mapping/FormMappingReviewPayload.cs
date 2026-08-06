using System;
using System.Collections.Generic;

namespace Unity.GrantManager.ApplicationForms.Mapping;

public sealed class FormMappingReviewPayload
{
    public List<FormMappingSuggestionDto> PendingSuggestions { get; set; } = [];
    public List<string> AcceptedWorksheetFields { get; set; } = [];
}
