using System.Collections.Generic;
using Unity.Flex.Worksheets;

namespace Unity.Flex.Web.Views.Shared.Components.Worksheets;

public class WorksheetWidgetViewModel
{
    public WorksheetDto Worksheet { get; set; } = new WorksheetDto();
    public Dictionary<string, string> IconMap { get; set; } = [];
}
