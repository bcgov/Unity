using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.TenantManagement;

namespace Unity.TenantManagement.Validation;

[RemoteService(false)]
[ExposeServices(typeof(IOnboardingValidationStep))]
public class TenantNameUniquenessStep(ITenantRepository tenantRepository)
    : IOnboardingValidationStep, ITransientDependency
{
    public int Order => 10;
    public string StepName => "Tenant Name";

    public async Task<OnboardingValidationStepResult> ValidateAsync(OnboardingRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.TenantName))
            return OnboardingValidationStepResult.Failure("Tenant name is required.");

        // FindByNameAsync matches against NormalizedName (stored as ToUpper())
        var existing = await tenantRepository.FindByNameAsync(request.TenantName.ToUpper());
        return existing is not null
            ? OnboardingValidationStepResult.Failure($"A tenant named '{request.TenantName}' already exists.")
            : OnboardingValidationStepResult.Success();
    }
}
