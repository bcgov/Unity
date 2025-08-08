using System;
using System.Collections.Generic;

namespace Unity.Flex.Web.Views.Shared.Components.WorksheetInstanceWidget.ViewModels;

public class WorksheetViewModel
{
    public string Name { get; set; } = string.Empty;
    public string UiAnchor { get; set; } = string.Empty;
    public bool IsConfigured { get; set; } = false;
    public Guid WorksheetId { get; set; }
    public Guid WorksheetInstanceId { get; set; }
    public List<WorksheetInstanceSectionViewModel> Sections { get; set; } = [];
    public List<Guid> WorksheetIds { get; set; } = [];
    public List<WorksheetViewModel> Worksheets { get; set; } = [];
    public bool HasMultipleWorksheets => Worksheets.Count > 1;
}
