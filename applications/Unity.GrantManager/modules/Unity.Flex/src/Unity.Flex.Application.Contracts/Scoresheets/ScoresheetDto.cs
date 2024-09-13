using System;
using System.Collections.ObjectModel;
using Volo.Abp.Application.Dtos;
namespace Unity.Flex.Scoresheets
{
    [Serializable]
    public class ScoresheetDto : ExtensibleFullAuditedEntityDto<Guid>
    {
        public string Title { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public uint Version { get; set; }
        public bool Published { get; set; }
        public Collection<ScoresheetSectionDto> Sections { get; private set; } = [];

    }
}
