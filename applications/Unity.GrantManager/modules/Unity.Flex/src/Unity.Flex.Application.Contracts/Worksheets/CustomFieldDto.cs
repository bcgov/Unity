using System;
using Volo.Abp.Application.Dtos;

namespace Unity.Flex.Worksheets
{
    [Serializable]
    public class CustomFieldDto : EntityDto<Guid>
    {
        public string Name { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public CustomFieldType Type { get; set; }
        public uint Order { get; set; }
        public bool Enabled { get; set; } = true;
        public string? Definition { get; set; } = "{}";
    }
}
