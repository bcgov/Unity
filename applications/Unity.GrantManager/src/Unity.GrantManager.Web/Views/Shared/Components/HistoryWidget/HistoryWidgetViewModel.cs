using System.Collections.Generic;
using Unity.GrantManager.History;

namespace Unity.GrantManager.Web.Views.Shared.Components.HistoryWidget
{
    public class HistoryWidgetViewModel
    {
        public IReadOnlyList<HistoryDto> ApplicationStatusHistoryList { get; set; }

        public HistoryWidgetViewModel()
        {
            ApplicationStatusHistoryList = new List<HistoryDto>();   
        }
    }
}
