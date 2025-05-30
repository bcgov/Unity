using System;
using System.Text.Json.Serialization;

namespace Unity.GrantManager.Intakes;

[Serializable]
public class FormSubmissionSummaryDto
{
    [JsonPropertyName("submissionId")]
    public Guid Id { get; set; }

    [JsonPropertyName("formId")]
    public Guid FormId { get; set; }

    [JsonPropertyName("formVersionId")]
    public Guid FormVersionId { get; set; }

    [JsonPropertyName("confirmationId")]
    public string ConfirmationId { get; set; } = string.Empty;

    [JsonPropertyName("formSubmissionStatusCode")]
    public string FormSubmissionStatusCode { get; set; } = string.Empty;

    [JsonPropertyName("createdBy")]
    public string CreatedBy { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("deleted")]
    public bool Deleted { get; set; }

    [JsonPropertyName("projectTitle")]
    public string ProjectTitle { get; set; } = string.Empty;

    [JsonPropertyName("projectLocation")]
    public string ProjectLocation { get; set; } = string.Empty;

    [JsonPropertyName("contactName")]
    public string ContactName { get; set; } = string.Empty;

    [JsonPropertyName("organizationLegalName")]
    public string OrganizationLegalName { get; set; } = string.Empty;

    [JsonPropertyName("lateEntry")]
    public bool LateEntry { get; set; }

    [JsonPropertyName("totalRequest")]
    public int? EligibleAmount { get; set; }

    [JsonPropertyName("eligibleCost")]
    public int? RequestedAmount { get; set; }

    [JsonPropertyName("submissionDate")]
    public DateTime? SubmissionDate { get; set; }

    [JsonPropertyName("name")]
    public string name { get; set; } = string.Empty;

    [JsonPropertyName("tenant")]
    public string tenant { get; set; } = string.Empty;

    [JsonPropertyName("form")]
    public string form { get; set; } = string.Empty;
}
