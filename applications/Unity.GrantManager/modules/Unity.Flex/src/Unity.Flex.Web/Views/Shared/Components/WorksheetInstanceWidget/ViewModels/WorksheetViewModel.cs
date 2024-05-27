using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;

public class WorksheetViewModel
{
    public string UiAnchor { get; set; } = string.Empty;
    public bool IsConfigured { get; set; } = false;
    public List<WorksheetInstanceSectionViewModel> Sections { get; set; } = [];

    [DataType(DataType.Date)] // Required for model binder to render date correctly
    public DateTime Date { get; set; } = DateTime.Now;
}
