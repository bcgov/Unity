using System;
using System.Collections.Generic;

namespace Unity.GrantManager.ApplicationForms.Mapping;

public sealed class FormWorksheetReviewPayload
{
    public List<Guid> DraftWorksheetIds { get; set; } = [];
    public bool NoSuggestionsGenerated { get; set; }
}
