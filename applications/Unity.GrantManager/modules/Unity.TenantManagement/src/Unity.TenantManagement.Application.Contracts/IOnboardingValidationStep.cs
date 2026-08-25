using System.Threading.Tasks;

namespace Unity.TenantManagement;

public interface IOnboardingValidationStep
{
    int Order { get; }
    string StepName { get; }
    Task<OnboardingValidationStepResult> ValidateAsync(OnboardingRequestDto request);
}
