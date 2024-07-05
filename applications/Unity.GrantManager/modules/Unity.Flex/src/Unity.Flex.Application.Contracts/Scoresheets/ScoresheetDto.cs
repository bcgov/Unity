using System;
using System.Collections.ObjectModel;
using Volo.Abp.Application.Dtos;
namespace Unity.Flex.Scoresheets
{
    [Serializable]
    public class ScoresheetDto : ExtensibleFullAuditedEntityDto<Guid>
    {
        public string Title { get; set; } = string.Empty;
        public Guid GroupId { get; set; }
        public uint Version { get; set; }
        public Collection<ScoresheetSectionDto> Sections { get; private set; } = [];

    }
}
