using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.TenantManagement;

namespace Unity.TenantManagement.Validation;

[RemoteService(false)]
[ExposeServices(typeof(IOnboardingValidationStep))]
public class TenantNameUniquenessStep(ITenantRepository tenantRepository, IDataFilter dataFilter)
    : IOnboardingValidationStep, ITransientDependency
{
    public int Order => 10;
    public string StepName => "Tenant Name";

    public async Task<OnboardingValidationStepResult> ValidateAsync(OnboardingRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.TenantName))
            return OnboardingValidationStepResult.Failure("Tenant name is required.");

        // FindByNameAsync matches against NormalizedName (stored as ToUpper()). Soft-deleted tenants
        // are included: one still holds its database and roles until it is purged.
        Tenant existing;
        using (dataFilter.Disable<ISoftDelete>())
        {
            existing = await tenantRepository.FindByNameAsync(request.TenantName.ToUpper());
        }

        if (existing is null)
            return OnboardingValidationStepResult.Success();

        return existing.IsDeleted
            ? OnboardingValidationStepResult.Failure($"A deleted tenant named '{request.TenantName}' still exists.")
            : OnboardingValidationStepResult.Failure($"A tenant named '{request.TenantName}' already exists.");
    }
}
