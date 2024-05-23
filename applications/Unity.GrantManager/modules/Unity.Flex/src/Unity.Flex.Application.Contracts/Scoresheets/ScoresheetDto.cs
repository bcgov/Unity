using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using Volo.Abp.Application.Dtos;
namespace Unity.Flex.Scoresheets
{
    [Serializable]
    public class ScoresheetDto : ExtensibleFullAuditedEntityDto<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public Guid GroupId { get; set; }
        public uint Version { get; set; }
        public Collection<uint> GroupVersions { get; set; } = [];
        public virtual Collection<ScoresheetSectionDto> Sections { get; private set; } = [];
    }
}
