using System;
using System.Collections.Generic;

namespace Unity.GrantManager.ApplicationForms.Mapping;

public class AiWorksheetReviewDto
{
    public Guid SessionId { get; set; }
    public List<AiWorksheetReviewFieldDto> Fields { get; set; } = [];
}

public class AiWorksheetReviewFieldDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool Selected { get; set; } = true;
}
