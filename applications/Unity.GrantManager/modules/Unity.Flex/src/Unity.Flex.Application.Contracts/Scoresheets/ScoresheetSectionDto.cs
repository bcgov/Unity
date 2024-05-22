using System;
using System.Collections.ObjectModel;
using Volo.Abp.Application.Dtos;


namespace Unity.Flex.Scoresheets
{
    [Serializable]
    public class ScoresheetSectionDto : ExtensibleEntityDto<Guid>
    {
        public virtual string Name { get; private set; } = string.Empty;
        public virtual uint Order { get; private set; }      
        public virtual Guid ScoresheetId { get; }

        public virtual Collection<QuestionDto> Fields { get; private set; } = [];


    }
}
