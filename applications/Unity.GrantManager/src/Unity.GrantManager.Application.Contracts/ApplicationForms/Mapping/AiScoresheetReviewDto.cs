using System;
using System.Collections.Generic;

namespace Unity.GrantManager.ApplicationForms.Mapping;

public sealed class AiScoresheetReviewDto
{
    public Guid SessionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public List<AiScoresheetReviewSectionDto> Sections { get; set; } = [];
}

public sealed class AiScoresheetReviewSectionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public uint Order { get; set; }
    public List<AiScoresheetReviewQuestionDto> Questions { get; set; } = [];
}

public sealed class AiScoresheetReviewQuestionDto
{
    public Guid Id { get; set; }
    public Guid SectionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public bool Selected { get; set; } = true;
}
