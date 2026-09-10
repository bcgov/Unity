using System;
using System.Collections.Generic;

namespace Unity.GrantManager.ApplicationForms.Mapping;

public class ApplicationFormMappingReadModelDto
{
    public Guid ApplicationFormVersionId { get; set; }
    public Guid ApplicationFormId { get; set; }
    public string? ChefsApplicationFormGuid { get; set; }
    public string? ChefsFormVersionGuid { get; set; }
    public string? ExistingMapping { get; set; }
    public List<MappingFieldDto> ChefsFields { get; set; } = [];
    public List<MappingFieldDto> UnityCoreFields { get; set; } = [];
    public List<WorksheetMappingFieldsDto> Worksheets { get; set; } = [];
}
