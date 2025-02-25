using System.Text.Json.Serialization;

namespace Unity.GrantManager.Attachments;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AttachmentType
{
    APPLICATION = 0,
    ASSESSMENT = 1,
    CHEFS = 2,
    EMAIL = 3
}
