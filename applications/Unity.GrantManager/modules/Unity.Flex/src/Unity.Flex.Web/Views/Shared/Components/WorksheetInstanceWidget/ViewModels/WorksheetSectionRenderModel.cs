using System;
using System.Collections.Generic;

namespace Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels
{
    public class WorksheetSectionRenderModel
    {
        public IEnumerable<WorksheetInstanceSectionViewModel> Sections { get; set; } = [];
        public string ModelName { get; set; } = string.Empty;
        public Guid WorksheetId { get; set; }
        public Guid WorksheetInstanceId { get; set; }
        public string UiAnchor { get; set; } = string.Empty;
    }
}
