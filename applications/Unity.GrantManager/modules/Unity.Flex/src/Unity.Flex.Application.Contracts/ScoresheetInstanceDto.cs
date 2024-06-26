using System;
using System.Collections.ObjectModel;
using Unity.Flex.Scoresheets;
using Volo.Abp.Application.Dtos;

namespace Unity.Flex
{
    [Serializable]
    public class ScoresheetInstanceDto : EntityDto<Guid>
    {
        public string Value { get; set; } = string.Empty;
        public Guid ScoresheetId { get; set; }
        public virtual Guid CorrelationId { get; set; }
        public virtual string CorrelationProvider { get; set; } = string.Empty;
        public virtual Collection<AnswerDto> Answers { get; private set; } = [];
    }
}
