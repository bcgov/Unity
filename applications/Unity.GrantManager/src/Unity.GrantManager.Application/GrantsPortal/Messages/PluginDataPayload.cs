using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Unity.GrantManager.GrantsPortal.Messages;

public class PluginDataPayload
{
    [JsonProperty("action")]
    public string Action { get; set; } = string.Empty;

    [JsonProperty("contactId")]
    public string? ContactId { get; set; }

    [JsonProperty("addressId")]
    public string? AddressId { get; set; }

    [JsonProperty("organizationId")]
    public string? OrganizationId { get; set; }

    [JsonProperty("profileId")]
    public string? ProfileId { get; set; }

    [JsonProperty("provider")]
    public string? Provider { get; set; }

    [JsonProperty("subject")]
    public string? Subject { get; set; }

    [JsonProperty("data")]
    public JObject? Data { get; set; }
}
