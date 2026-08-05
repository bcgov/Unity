using System.Collections.Generic;

namespace Unity.GrantManager.ApplicationForms.Mapping;

public class FormWorksheetDto
{
    public string WorksheetName { get; set; } = string.Empty;
    public List<FormMappingDto> FieldMatches { get; set; } = [];
}
