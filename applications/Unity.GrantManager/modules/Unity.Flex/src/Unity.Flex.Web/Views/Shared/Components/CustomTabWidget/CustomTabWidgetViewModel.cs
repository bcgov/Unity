using System;

namespace Unity.GrantManager.Web.Views.Shared.Components.CustomTabWidget
{
    public class CustomTabWidgetViewModel
    {
        public Guid InstanceCorrelationId { get; internal set; }
        public string InstanceCorrelationProvider { get; internal set; } = string.Empty;
        public Guid SheetCorrelationId { get; internal set; }
        public string SheetCorrelationProvider { get; internal set; } = string.Empty;
        public string UiAnchor { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public Guid WorksheetId { get; set; }
    }
}