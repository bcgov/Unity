using System;
using Volo.Abp.Application.Dtos;

namespace Unity.Flex.Scoresheets
{
    [Serializable]
    public class AnswerDto : EntityDto<Guid>
    {
        public string? CurrentValue { get; set; } = string.Empty;
        public uint Version { get; set; }
        public Guid QuestionId { get; set; }
        public Guid ScoresheetInstanceId { get; set; }
    }
}
