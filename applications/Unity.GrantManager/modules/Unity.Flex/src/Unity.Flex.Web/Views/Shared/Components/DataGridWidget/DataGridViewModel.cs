using System.Collections.Generic;

namespace Unity.Flex.Web.Views.Shared.Components.DataGridWidget
{
    public class DataGridViewModel : WorksheetViewModelBase
    {
        public string[] Columns { get; set; } = [];
        public DataGridViewModelRow[] Rows { get; set; } = [];
        public DataGridViewSummary Summary { get; set; } = new DataGridViewSummary();

        public DataGridViewModel() : base()
        {
        }
    }

    public class DataGridViewModelRow
    {
        public List<DataGridViewModelCell> Cells { get; set; } = [];
    }

    public class DataGridViewModelCell
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public class DataGridViewSummary
    {
        public List<DataGridViewModelSummaryField> Fields { get; set; } = [];
    }

    public class DataGridViewModelSummaryField
    {
        public string Label { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
    }
}


