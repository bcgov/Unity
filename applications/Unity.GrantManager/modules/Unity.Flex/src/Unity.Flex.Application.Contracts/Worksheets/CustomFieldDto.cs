using System;
using Volo.Abp.Application.Dtos;

namespace Unity.Flex.Worksheets
{
    [Serializable]
    public class CustomFieldDto : EntityDto
    {
        public string Name { get; set; } = string.Empty;
    }
}
