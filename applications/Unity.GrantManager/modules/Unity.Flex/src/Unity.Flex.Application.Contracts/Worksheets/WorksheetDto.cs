using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace Unity.Flex.Worksheets
{
    [Serializable]
    public class WorksheetDto : ExtensibleFullAuditedEntityDto<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public string UiAnchor { get; set; } = string.Empty;
        public virtual List<WorksheetSectionDto> Sections { get; set; } = [];
    }
}
