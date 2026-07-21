using System;
using System.Collections.Generic;

namespace Unity.GrantManager.ApplicationForms.Mapping;

public class ConfirmAiWorksheetDto
{
    public Guid WorksheetId { get; set; }
    public List<Guid> SelectedFieldIds { get; set; } = [];
}
