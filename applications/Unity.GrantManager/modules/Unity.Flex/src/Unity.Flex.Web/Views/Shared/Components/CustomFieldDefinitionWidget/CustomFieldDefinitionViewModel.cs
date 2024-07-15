using Unity.Flex.Worksheets;

namespace Unity.Flex.Web.Views.Shared.Components.CustomFieldDefinitionWidget
{
    public class CustomFieldDefinitionViewModel
    {
        public CustomFieldDefinitionViewModel()
        {
        }

        public CustomFieldType Type { get; set; }
        public string? Definition { get; set; }
    }
}
