using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.ApplicationForms.Mapping;

public sealed class CreateAiScoresheetDraftDto
{
    public Guid SessionId { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    [MinLength(1)]
    public List<Guid> SelectedQuestionIds { get; set; } = [];
}
