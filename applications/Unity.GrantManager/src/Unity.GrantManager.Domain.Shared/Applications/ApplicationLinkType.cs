using System.Text.Json.Serialization;

namespace Unity.GrantManager.Applications;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ApplicationLinkType
{
    Related,
    Parent,
    Child
}