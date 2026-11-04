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
    public bool IsApproved { get; set; }
    public string Category { get; set; } = string.Empty;
    public Guid? OwnerId { get; set; }
    public string? OwnerName { get; set; }
    public List<OnboardingAssigneeDto> Assignees { get; set; } = [];
    public Dictionary<string, object?> CoreFieldValues { get; set; } = [];
}
