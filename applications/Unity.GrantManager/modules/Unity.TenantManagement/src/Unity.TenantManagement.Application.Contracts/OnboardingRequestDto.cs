#nullable enable
using System;
using System.Collections.Generic;

namespace Unity.TenantManagement;

public class OnboardingRequestDto
{
    public Guid Id { get; set; }
    public string SubmissionNumber { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public string TenantDescription { get; set; } = string.Empty;
    public string ProgramAreaName { get; set; } = string.Empty;
    public string ProgramAreaDescription { get; set; } = string.Empty;
    public string Contacts { get; set; } = string.Empty;
    public string Features { get; set; } = string.Empty;
    public string SuperUsers { get; set; } = string.Empty;
    public string ExecutiveDirector { get; set; } = string.Empty;
    public string Branch { get; set; } = string.Empty;
    public string Ministry { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime? SubmissionDate { get; set; }
    public Dictionary<string, object?> Fields { get; set; } = [];
}
