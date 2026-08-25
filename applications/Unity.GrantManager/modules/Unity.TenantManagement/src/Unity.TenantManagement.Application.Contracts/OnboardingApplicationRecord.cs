#nullable enable
using System;
using System.Collections.Generic;

namespace Unity.TenantManagement;

public class OnboardingApplicationRecord
{
    public Guid Id { get; set; }
    public string ReferenceNo { get; set; } = string.Empty;
    public DateTime SubmissionDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public Dictionary<string, object?> CoreFieldValues { get; set; } = [];
}
