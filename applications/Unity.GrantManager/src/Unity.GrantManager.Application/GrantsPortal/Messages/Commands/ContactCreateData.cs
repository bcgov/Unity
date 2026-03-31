using Newtonsoft.Json;

namespace Unity.GrantManager.GrantsPortal.Messages.Commands;

public class ContactCreateData
{
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("email")]
    public string Email { get; set; } = string.Empty;

    [JsonProperty("title")]
    public string? Title { get; set; }

    [JsonProperty("contactType")]
    public string? ContactType { get; set; }

    [JsonProperty("homePhoneNumber")]
    public string? HomePhoneNumber { get; set; }

    [JsonProperty("mobilePhoneNumber")]
    public string? MobilePhoneNumber { get; set; }

    [JsonProperty("workPhoneNumber")]
    public string? WorkPhoneNumber { get; set; }

    [JsonProperty("workPhoneExtension")]
    public string? WorkPhoneExtension { get; set; }

    [JsonProperty("role")]
    public string? Role { get; set; }

    [JsonProperty("isPrimary")]
    public bool IsPrimary { get; set; }
}
