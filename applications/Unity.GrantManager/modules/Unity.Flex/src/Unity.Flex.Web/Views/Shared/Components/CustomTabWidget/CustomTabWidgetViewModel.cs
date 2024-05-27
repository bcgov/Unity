using JetBrains.Annotations;
using System;

namespace Unity.GrantManager.Web.Views.Shared.Components.CustomTabWidget
{
    public class CustomTabWidgetViewModel
    {
        public Guid CorrelationId { get; set; }
        public string CorrelationProvider { get; set; } = string.Empty;
        public string UiAnchor { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}