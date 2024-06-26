using System;

namespace Unity.Flex.Worksheets
{
    [Serializable]
    public class EditCustomFieldDto
    {
        public string Field { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public CustomFieldType Type { get; set; }
        public object? Definition { get; set; }
    }
}
