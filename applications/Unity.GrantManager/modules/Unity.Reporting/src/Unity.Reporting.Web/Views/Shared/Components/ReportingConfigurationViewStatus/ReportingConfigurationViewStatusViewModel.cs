using System;
using Unity.Reporting.Configuration;

namespace Unity.Reporting.Web.Views.Shared.Components.ReportingConfigurationViewStatus
{
    public class ReportingConfigurationViewStatusViewModel
    {
        public Guid VersionId { get; set; }
        public string Provider { get; set; } = "formversion";
        public string ViewName { get; set; } = string.Empty;
        public ViewStatus? ViewStatus { get; set; }
        public bool HasMapping { get; set; }
    }
}