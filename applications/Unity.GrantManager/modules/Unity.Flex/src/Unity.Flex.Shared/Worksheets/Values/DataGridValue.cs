using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Unity.Flex.Worksheets.Values
{
    public class DataGridValue : CustomValueBase
    {
        public DataGridValue() : base() { }
        public DataGridValue(object value) : base(value) { }

        [JsonPropertyName("columns")]
        public List<DataGridColumn> Columns { get; set; } = [];
    }

    public class DataGridRowsValue
    {
        public DataGridRowsValue()
        {
        }
        public DataGridRowsValue(List<DataGridRow> rows)
        {
            Rows = rows;
        }

        [JsonPropertyName("rows")]
        public List<DataGridRow> Rows { get; set; } = [];
    }

    public class DataGridRow
    {
        [JsonPropertyName("cells")]
        public List<DataGridRowCell> Cells { get; set; } = [];
    }

    public class DataGridRowCell
    {
        public DataGridRowCell()
        {
        }

        public DataGridRowCell(string key, string value)
        {
            Key = key;
            Value = value;
        }

        [JsonPropertyName("key")]
        public string? Key { get; set; }

        [JsonPropertyName("value")]
        public string? Value { get; set; }
    }

    public class DataGridColumn
    {
        [JsonPropertyName("key")]
        public string? Key { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("format")]
        public string? Format { get; set; }

        public DataGridColumn()
        {
        }

        public DataGridColumn(string key, string name, string type, string? format)
        {
            Key = key;
            Name = name;
            Type = type;
            Format = format;
        }
    }
}
