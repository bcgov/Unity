#nullable enable
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.TenantManagement;

[Controller]
[RemoteService(Name = TenantManagementRemoteServiceConsts.RemoteServiceName)]
[Area(TenantManagementRemoteServiceConsts.ModuleName)]
[Route("api/multi-tenancy/onboarding-requests")]
public class OnboardingRequestController(IOnboardingRequestAppService onboardingRequestAppService)
    : AbpControllerBase, IOnboardingRequestAppService
{
    protected IOnboardingRequestAppService OnboardingRequestAppService { get; } = onboardingRequestAppService;

    [HttpGet]
    public virtual Task<PagedResultDto<OnboardingRequestDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        if (!ModelState.IsValid) throw new UserFriendlyException("OnboardingRequestController->GetListAsync: ModelState Invalid");
        return OnboardingRequestAppService.GetListAsync(input);
    }

    [HttpGet("{id}")]
    public virtual Task<OnboardingRequestDto?> GetAsync(Guid id)
    {
        if (!ModelState.IsValid) throw new UserFriendlyException("OnboardingRequestController->GetAsync: ModelState Invalid");
        return OnboardingRequestAppService.GetAsync(id);
    }

    [HttpGet("{id}/validate")]
    public virtual Task<OnboardingValidationResultDto> ValidateAsync(Guid id)
    {
        if (!ModelState.IsValid) throw new UserFriendlyException("OnboardingRequestController->ValidateAsync: ModelState Invalid");
        return OnboardingRequestAppService.ValidateAsync(id);
    }

    [HttpPost("{id}/create-tenant")]
    public virtual Task CreateTenantAsync(Guid id)
    {
        if (!ModelState.IsValid) throw new UserFriendlyException("OnboardingRequestController->CreateTenantAsync: ModelState Invalid");
        return OnboardingRequestAppService.CreateTenantAsync(id);
    }
}
