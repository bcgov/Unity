using System;
using Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;

namespace Unity.Flex.Web.Views.Shared.Components
{
    public class WorksheetViewModelBase
    {
        protected WorksheetViewModelBase() { }
        public WorksheetFieldViewModel? Field { get; internal set; }
        public string Name { get; internal set; } = string.Empty;
        public Guid? WorksheetId { get; internal set; }
    }
}
