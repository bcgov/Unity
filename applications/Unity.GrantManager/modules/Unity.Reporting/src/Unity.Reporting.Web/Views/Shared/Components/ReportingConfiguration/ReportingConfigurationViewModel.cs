using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using Unity.Reporting.Configuration;

namespace Unity.Reporting.Web.Views.Shared.Components.ReportingConfiguration
{
    public class ReportingConfigurationViewModel
    {
        public List<SelectListItem> FormVersions { get; set; } = new();
        public Guid? SelectedVersionId { get; set; }
        public Guid FormId { get; set; }
        public string ViewName { get; set; } = string.Empty;
        public ViewStatus? ViewStatus { get; set; }
        public bool HasSavedConfiguration { get; set; } = false;
        public bool HasDuplicateKeys { get; set; } = false;
    }
}