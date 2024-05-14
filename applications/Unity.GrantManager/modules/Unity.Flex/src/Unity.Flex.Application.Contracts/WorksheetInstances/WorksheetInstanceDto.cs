using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace Unity.Flex.WorksheetInstances
{
    [Serializable]
    public class WorksheetInstanceDto : EntityDto<Guid>
    {
        public virtual List<CustomFieldValueDto> Values { get; private set; } = [];
    }
}
