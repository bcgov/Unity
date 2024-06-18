using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;

namespace Unity.Flex.Web.Views.Shared.Components
{
    public abstract class WorksheetViewModelBase
    {
        public WorksheetFieldViewModel? Field { get; internal set; }
        public string Name { get; internal set; } = string.Empty;
    }
}
