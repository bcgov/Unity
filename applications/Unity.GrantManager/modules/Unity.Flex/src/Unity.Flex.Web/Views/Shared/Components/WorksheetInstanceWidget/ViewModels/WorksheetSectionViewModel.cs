using System.Collections.Generic;

namespace Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels
{
    public class WorksheetInstanceSectionViewModel
    {
        public string Name { get; set; } = string.Empty;
        public List<WorksheetFieldViewModel> Fields { get; set; } = [];
    }
}