using Newtonsoft.Json;

namespace Unity.GrantManager.GrantsPortal.Messages.Commands;

public abstract class AddressDataBase
{
    [JsonProperty("addressType")]
    public string? AddressType { get; set; }

    [JsonProperty("street")]
    public string Street { get; set; } = string.Empty;

    [JsonProperty("street2")]
    public string? Street2 { get; set; }

    [JsonProperty("unit")]
    public string? Unit { get; set; }

    [JsonProperty("city")]
    public string City { get; set; } = string.Empty;

    [JsonProperty("province")]
    public string Province { get; set; } = string.Empty;

    [JsonProperty("postalCode")]
    public string PostalCode { get; set; } = string.Empty;

    [JsonProperty("country")]
    public string? Country { get; set; }

    [JsonProperty("isPrimary")]
    public bool IsPrimary { get; set; }
}
