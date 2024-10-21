using Unity.Flex.Worksheets;

namespace Unity.Flex.Web.Views.Shared.Components
{
    public class WorksheetFieldDefinitionViewModelBase
    {
        protected WorksheetFieldDefinitionViewModelBase() { }
        public CustomFieldType Type { get; set; }
        public string? Definition { get; set; }
    }
}
