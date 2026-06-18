#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.TenantManagement;

[Controller]
[RemoteService(Name = TenantManagementRemoteServiceConsts.RemoteServiceName)]
[Area(TenantManagementRemoteServiceConsts.ModuleName)]
[Route("api/onboarding-requests")]
public class OnboardingRequestController(IOnboardingRequestAppService onboardingRequestAppService)
    : AbpControllerBase, IOnboardingRequestAppService
{
    protected IOnboardingRequestAppService OnboardingRequestAppService { get; } = onboardingRequestAppService;

    [HttpGet]
    public virtual Task<PagedResultDto<OnboardingRequestDto>> GetListAsync([FromQuery] OnboardingListRequestDto input)
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
    public virtual Task<OnboardingValidationResultDto> ValidateAsync(
        Guid id,
        [FromQuery] string? tenantNameFieldKey = null,
        [FromQuery] string? superUsersFieldKey = null,
        [FromQuery] string? branchFieldKey = null,
        [FromQuery] string? featuresFieldKey = null,
        [FromQuery] string? ministryFieldKey = null,
        [FromQuery] string? programAreaFieldKey = null)
    {
        if (!ModelState.IsValid) throw new UserFriendlyException("OnboardingRequestController->ValidateAsync: ModelState Invalid");
        return OnboardingRequestAppService.ValidateAsync(id, tenantNameFieldKey, superUsersFieldKey, branchFieldKey, featuresFieldKey, ministryFieldKey, programAreaFieldKey);
    }

    [HttpPost("{id}/create-tenant")]
    public virtual Task CreateTenantAsync(Guid id, [FromBody] CreateTenantInputDto? input = null)
    {
        if (!ModelState.IsValid) throw new UserFriendlyException("OnboardingRequestController->CreateTenantAsync: ModelState Invalid");
        return OnboardingRequestAppService.CreateTenantAsync(id, input);
    }

    [HttpGet("column-schema")]
    public virtual Task<OnboardingColumnSchemaDto> GetColumnSchemaAsync([FromQuery] string? category = null)
    {
        if (!ModelState.IsValid) throw new UserFriendlyException("OnboardingRequestController->GetColumnSchemaAsync: ModelState Invalid");
        return OnboardingRequestAppService.GetColumnSchemaAsync(category);
    }

    [HttpGet("categories")]
    public virtual Task<List<string>> GetAvailableCategoriesAsync()
    {
        if (!ModelState.IsValid) throw new UserFriendlyException("OnboardingRequestController->GetAvailableCategoriesAsync: ModelState Invalid");
        return OnboardingRequestAppService.GetAvailableCategoriesAsync();
    }
}
