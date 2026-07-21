using System;
using System.Collections.Generic;

namespace Unity.GrantManager.ApplicationForms.Mapping;

public class AiWorksheetReviewDto
{
    public Guid WorksheetId { get; set; }
    public string WorksheetName { get; set; } = string.Empty;
    public string WorksheetTitle { get; set; } = string.Empty;
    public List<AiWorksheetReviewSectionDto> Sections { get; set; } = [];
}

public class AiWorksheetReviewSectionDto
{
    public string Name { get; set; } = string.Empty;
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
