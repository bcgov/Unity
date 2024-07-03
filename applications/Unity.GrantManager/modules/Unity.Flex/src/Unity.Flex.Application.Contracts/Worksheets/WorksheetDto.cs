using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace Unity.Flex.Worksheets
{
    [Serializable]
    public class WorksheetDto : ExtensibleFullAuditedEntityDto<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public virtual List<WorksheetSectionDto> Sections { get; set; } = [];
        public uint TotalFields { get; set; } = 0;
        public uint TotalSections { get; set; } = 0;
        public uint Version { get; set; } = 0;
        public bool Published { get; set; } = false;
    }
}
