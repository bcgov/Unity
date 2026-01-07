using System;
using System.Collections.Generic;
using Unity.Flex.Worksheets.Definitions;

namespace Unity.Flex.Web.Views.Shared.Components.DataGridWidget
{
    public class DataGridViewModel : WorksheetViewModelBase
    {
        public bool AllowEdit { get; set; }
        public string[] Columns { get; set; } = [];
        public string TableOptions { get; set; } = string.Empty;
        public DataGridViewModelRow[] Rows { get; set; } = [];
        public DataGridDefinitionSummaryOption SummaryOption { get; set; }
        public DataGridViewSummary Summary { get; set; } = new DataGridViewSummary();
        public Guid WorksheetInstanceId { get; set; }
        public new Guid WorksheetId { get; set; }
        public string UiAnchor { get; set; } = string.Empty;

        public DataGridViewModel() : base()
        {
        }
    }

    public class DataGridViewModelRow
    {
        public uint RowNumber { get; set; }
        public List<DataGridViewModelCell> Cells { get; set; } = [];
    }

    public class DataGridViewModelCell
    {
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public class DataGridViewSummary
    {
        public List<DataGridViewModelSummaryField> Fields { get; set; } = [];
    }

    public class DataGridViewModelSummaryField
    {
        public string Label { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}


