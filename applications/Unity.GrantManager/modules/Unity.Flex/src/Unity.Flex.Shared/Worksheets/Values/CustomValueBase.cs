using System.Text.Json.Serialization;

namespace Unity.Flex.Worksheets.Values
{
    public abstract class CustomValueBase
    {
        protected CustomValueBase()
        {
        }

        protected CustomValueBase(object value)
        {
            Value = value;
        }

        [JsonPropertyName("value")]
        public object? Value { get; set; }
    }
}
