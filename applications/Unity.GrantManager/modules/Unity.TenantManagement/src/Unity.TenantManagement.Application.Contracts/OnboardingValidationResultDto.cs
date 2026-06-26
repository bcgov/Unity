using System.Collections.Generic;

namespace Unity.TenantManagement;

public class OnboardingValidationResultDto
{
    public bool IsValid { get; set; }
    public List<string> Issues { get; set; } = [];
}
