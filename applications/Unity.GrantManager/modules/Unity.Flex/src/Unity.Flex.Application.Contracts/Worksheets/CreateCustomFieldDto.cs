using System;
using Unity.Flex.Worksheets.Definitions;

namespace Unity.Flex.Worksheets
{
    [Serializable]
    public class CreateCustomFieldDto
    {
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public CustomFieldType Type { get; set; }
        public object? Definition { get; set; }
    }
}
