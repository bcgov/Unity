using System.Text.Json.Serialization;

namespace Unity.Flex.Worksheets.Definitions.Interfaces
{
    public interface ICustomFieldFormat
    {
        [JsonPropertyName("format")]
        public string Format { get; set; }
    }
}
