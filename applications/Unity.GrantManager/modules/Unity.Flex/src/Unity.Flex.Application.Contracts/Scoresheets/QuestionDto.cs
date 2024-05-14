using System;
using Volo.Abp.Application.Dtos;

namespace Unity.Flex.Scoresheets
{
    [Serializable]
    public class QuestionDto : ExtensibleEntityDto<Guid>
    {
        public virtual string Name { get; private set; } = string.Empty;
        public virtual string Label { get; private set; } = string.Empty;
        public virtual bool Enabled { get; private set; }

        public virtual Guid SectionId { get; }

              
    }
}