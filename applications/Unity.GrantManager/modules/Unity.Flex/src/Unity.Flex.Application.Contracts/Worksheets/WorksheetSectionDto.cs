using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace Unity.Flex.Worksheets
{
    [Serializable]
    public class WorksheetSectionDto : EntityDto<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public uint Order { get; set; }
        public List<CustomFieldDto> Fields { get; set; } = [];
    }
}
