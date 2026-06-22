#nullable enable
namespace Unity.TenantManagement;

public class OnboardingValidationStepResult
{
    public bool IsValid { get; set; }
    public string? Issue { get; set; }

    public static OnboardingValidationStepResult Success() => new() { IsValid = true };
    public static OnboardingValidationStepResult Failure(string issue) => new() { IsValid = false, Issue = issue };
}
