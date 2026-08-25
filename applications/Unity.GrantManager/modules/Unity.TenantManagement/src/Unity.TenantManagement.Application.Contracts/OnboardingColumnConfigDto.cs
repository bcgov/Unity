#nullable enable
using System.Collections.Generic;

namespace Unity.TenantManagement;

public class OnboardingColumnDto
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool Selected { get; set; }
}

public class OnboardingColumnSchemaDto
{
    public List<OnboardingColumnDto> Columns { get; set; } = [];
    public string? TenantNameFieldKey { get; set; }
    public string? SuperUsersFieldKey { get; set; }
    public string? BranchFieldKey { get; set; }
    public string? FeaturesFieldKey { get; set; }
    public string? MinistryFieldKey { get; set; }
    public string? ProgramAreaFieldKey { get; set; }
}

public class CreateTenantInputDto
{
    public string? TenantNameFieldKey { get; set; }
    public string? SuperUsersFieldKey { get; set; }
    public string? BranchFieldKey { get; set; }
    public string? FeaturesFieldKey { get; set; }
    public string? MinistryFieldKey { get; set; }
    public string? ProgramAreaFieldKey { get; set; }
}
