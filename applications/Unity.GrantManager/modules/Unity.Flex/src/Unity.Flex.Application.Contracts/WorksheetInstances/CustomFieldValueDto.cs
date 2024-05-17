using System;
using Volo.Abp.Application.Dtos;

namespace Unity.Flex.WorksheetInstances
{
    [Serializable]
    public class CustomFieldValueDto : EntityDto<Guid>
    {
        public virtual string? CurrentValue { get; set; } = "{}";
        public Guid WorksheetInstanceId { get; set; }
        public Guid CustomFieldId { get; set; }
    }
}
