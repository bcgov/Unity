using System.Text.Json.Serialization;

namespace Unity.ApplicantPortal.Web.Services;

public sealed class GrantManagerClient(HttpClient client)
{
    public async Task<IEnumerable<PublicProfile>?> GetApplicantProfile()
        => await client.GetFromJsonAsync<IEnumerable<PublicProfile>>("profile");

    public async Task<IEnumerable<PublicSubmission>?> GetApplicantSubmissions()
        => await client.GetFromJsonAsync<IEnumerable<PublicSubmission>>("submissions");

    public async Task<IEnumerable<PublicTenant>?> GetTenants()
        => await client.GetFromJsonAsync<IEnumerable<PublicTenant>>("tenants");
}

public record PublicProfile(
    [property: JsonPropertyName("organizationId")] int Id);

public record PublicSubmission(
    [property: JsonPropertyName("submissionId")] Guid SubmissionId,
    [property: JsonPropertyName("referenceNo")] string ReferenceNo,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("submissionDate")] DateTime SubmissionDate);

public record PublicTenant(
    [property: JsonPropertyName("tenantId")] int TenantId,
    [property: JsonPropertyName("name")] string TenantName);
