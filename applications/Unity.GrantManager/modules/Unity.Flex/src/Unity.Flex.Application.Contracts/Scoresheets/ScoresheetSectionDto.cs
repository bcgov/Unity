using System;
using System.Collections.ObjectModel;
using Volo.Abp.Application.Dtos;


namespace Unity.Flex.Scoresheets
{
    [Serializable]
    public class ScoresheetSectionDto : ExtensibleEntityDto<Guid>
    {
        public virtual string Name { get; set; } = string.Empty;
        public virtual uint Order { get; set; } = 0;
        public virtual Guid ScoresheetId { get; set; }
        public virtual Collection<QuestionDto> Fields { get; set; } = [];
    }
}
