using System;
using System.Collections.Generic;

namespace Unity.GrantManager.ApplicationForms.Mapping;

public sealed class AcceptMappingSuggestionsDto
{
    public List<Guid> SuggestionIds { get; set; } = [];
}
