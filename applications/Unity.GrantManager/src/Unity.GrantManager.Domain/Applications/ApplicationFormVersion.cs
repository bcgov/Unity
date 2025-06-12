using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Applications;

public class ApplicationFormVersion : AuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid ApplicationFormId { get; set; }
    public string? ChefsApplicationFormGuid { get; set; }
    public string? ChefsFormVersionGuid { get; set; }
    public string? SubmissionHeaderMapping { get; set; }
    public string? AvailableChefsFields { get; set; }
    public int? Version { get; set; }
    public bool Published { get; set; }
    public Guid? TenantId { get; set; }
    public string ReportColumns { get; set; } = string.Empty;
    public string ReportKeys { get; set; } = string.Empty;
    public string ReportViewName { get; set; } = string.Empty;
    [Column(TypeName = "jsonb")]
    public string? FormSchema { get; set; } = string.Empty;

    /// <summary>
    /// Checks if the submission header mapping contains a specific field.
    /// </summary>
    /// <param name="field"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public bool HasSubmissionHeaderMapping(string field)
    {
        if (string.IsNullOrWhiteSpace(SubmissionHeaderMapping))
            return false;

        try
        {
            var mapping = JsonSerializer.Deserialize<Dictionary<string, string>>(SubmissionHeaderMapping);
            return mapping != null && mapping.ContainsKey(field);
        }
        catch (JsonException)
        {
            // Optionally log or handle malformed JSON
            return false;
        }
    }
}
