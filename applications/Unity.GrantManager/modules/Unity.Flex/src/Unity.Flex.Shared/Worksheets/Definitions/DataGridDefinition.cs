using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Unity.Flex.Worksheets.Definitions
{
    public class DataGridDefinition : CustomFieldDefinition
    {
        public DataGridDefinition() : base()
        {
        }

        [JsonPropertyName("dynamic")]
        public bool Dynamic { get; set; }


        [JsonPropertyName("columns")]
        public List<DataGridDefinitionColumn> Columns { get; set; } = [];

        [JsonPropertyName("summaryOption")]
        public string SummaryOption { get; set; } = DataGridDefinitionSummaryOption.None.ToString();
    }

    public class DataGridDefinitionColumn
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;

        [JsonPropertyName("key")]
        public string Key { get; set; } = string.Empty;
    }

    public enum DataGridDefinitionSummaryOption
    {
        None,
        Above,
        Below
    }
}
