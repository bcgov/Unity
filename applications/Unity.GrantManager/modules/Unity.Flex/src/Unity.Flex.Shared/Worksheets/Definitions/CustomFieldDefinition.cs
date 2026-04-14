using System.Text.Json.Serialization;

namespace Unity.Flex.Worksheets.Definitions
{
    public class CustomFieldDefinition
    {
        [JsonPropertyName("required")]
        public bool Required { get; set; } = false;

        [JsonPropertyName("isHidden")]
        public bool IsHidden { get; set; } = false;

        [JsonPropertyName("hideLabel")]
        public bool HideLabel { get; set; } = false;

        [JsonPropertyName("isDisabled")]
        public bool IsDisabled { get; set; } = false;

        [JsonPropertyName("labelPosition")]
        public string LabelPosition { get; set; } = "Top";

        [JsonPropertyName("style")]
        public string? Style { get; set; }

        [JsonPropertyName("cssClass")]
        public string? CssClass { get; set; }

        [JsonPropertyName("labelStyle")]
        public string? LabelStyle { get; set; }

        [JsonPropertyName("labelCssClass")]
        public string? LabelCssClass { get; set; }

        [JsonPropertyName("securityClassification")]
        public string? SecurityClassification { get; set; }

        public CustomFieldDefinition() : base()
        {
        }
    }
}
