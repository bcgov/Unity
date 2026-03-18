using Newtonsoft.Json;

namespace Unity.GrantManager.GrantsPortal.Messages.Commands;

public class OrganizationEditData
{
    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("organizationType")]
    public string? OrganizationType { get; set; }

    [JsonProperty("organizationNumber")]
    public string? OrganizationNumber { get; set; }

    [JsonProperty("status")]
    public string? Status { get; set; }

    [JsonProperty("nonRegOrgName")]
    public string? NonRegOrgName { get; set; }

    [JsonProperty("fiscalMonth")]
    public string? FiscalMonth { get; set; }

    [JsonProperty("fiscalDay")]
    public string? FiscalDay { get; set; }

    [JsonProperty("organizationSize")]
    public string? OrganizationSize { get; set; }
}
