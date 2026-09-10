using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.ApplicationForms.Mapping;

public class CreateAiWorksheetDraftDto
{
    public Guid SessionId { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;

    [MinLength(1)]
    public List<Guid> SelectedFieldIds { get; set; } = [];
}
