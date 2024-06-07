using System;
using Volo.Abp.Application.Dtos;

namespace Unity.Flex.Scoresheets
{
    [Serializable]
    public class QuestionDto : ExtensibleEntityDto<Guid>
    {
        public virtual string Name { get; set; } = string.Empty;
        public virtual string Label { get; set; } = string.Empty;
        public virtual string? Description { get; set; }
        public virtual bool Enabled { get; private set; }
        public virtual uint Order { get; set; }

        public virtual Guid SectionId { get; }

              
    }
}