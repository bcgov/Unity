using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Unity.Flex.Domain.WorksheetInstances
{
    public class WorksheetInstanceValue
    {
        [JsonPropertyName("values")]
        public List<FieldInstanceValue> Values { get; set; } = [];
    }
}
