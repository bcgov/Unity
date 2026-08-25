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

    // Kept for Grants Portal backward compatibility — portal sends "organizationSize" in the PUT payload.
    // OrganizationEditHandler falls back to this value when ApproxNumberOfEmployees is not present.
    [JsonProperty("organizationSize")]
    public string? OrganizationSize { get; set; }

    // Preferred field — portal developer should migrate the PUT payload to send "approxNumberOfEmployees"
    // instead of "organizationSize" after deployment. Handler uses this when present.
    [JsonProperty("approxNumberOfEmployees")]
    public string? ApproxNumberOfEmployees { get; set; }
}
