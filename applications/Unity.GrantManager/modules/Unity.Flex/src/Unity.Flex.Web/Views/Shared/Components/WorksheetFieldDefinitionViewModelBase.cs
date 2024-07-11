using Unity.Flex.Worksheets;

namespace Unity.Flex.Web.Views.Shared.Components
{
    public abstract class WorksheetFieldDefinitionViewModelBase
    {
        public CustomFieldType Type { get; set; }
        public string? Definition { get; set; }
    }
}
