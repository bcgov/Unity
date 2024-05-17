using System;
using System.Collections.Generic;

namespace Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;

public class WorksheetViewModel
{
    public string UiAnchor { get; set; } = string.Empty;
    public bool IsConfigured { get; set; } = false;
    public List<WorksheetInstanceSectionViewModel> Sections { get; set; } = [];
}
