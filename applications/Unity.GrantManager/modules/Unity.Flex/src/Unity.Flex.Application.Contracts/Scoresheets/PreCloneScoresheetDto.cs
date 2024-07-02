using System;
using System.Collections.ObjectModel;
using Volo.Abp.Application.Dtos;
namespace Unity.Flex.Scoresheets
{
    [Serializable]
    public class PreCloneScoresheetDto : ExtensibleFullAuditedEntityDto<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public Guid GroupId { get; set; }
        public uint HighestVersion { get; set; }

    }
}
