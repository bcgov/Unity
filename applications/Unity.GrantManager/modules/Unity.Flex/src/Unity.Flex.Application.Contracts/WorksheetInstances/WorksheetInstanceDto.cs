using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace Unity.Flex.WorksheetInstances
{
    [Serializable]
    public class WorksheetInstanceDto : EntityDto<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public virtual List<WorksheetInstanceSectionDto> Sections { get; set; } = [];
    }
}
