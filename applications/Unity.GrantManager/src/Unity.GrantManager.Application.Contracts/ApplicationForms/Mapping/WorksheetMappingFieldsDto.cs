using System;
using System.Collections.Generic;

namespace Unity.GrantManager.ApplicationForms.Mapping;

public class WorksheetMappingFieldsDto
{
    public Guid WorksheetId { get; set; }
    public string WorksheetName { get; set; } = string.Empty;
    public List<MappingFieldDto> Fields { get; set; } = [];
}
