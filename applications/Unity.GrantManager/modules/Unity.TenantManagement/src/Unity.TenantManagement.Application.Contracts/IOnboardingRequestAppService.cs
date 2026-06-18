#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Unity.TenantManagement;

public interface IOnboardingRequestAppService : IApplicationService
{
    Task<PagedResultDto<OnboardingRequestDto>> GetListAsync(OnboardingListRequestDto input);
    Task<OnboardingRequestDto?> GetAsync(Guid id);
    Task<OnboardingValidationResultDto> ValidateAsync(Guid id, string? tenantNameFieldKey, string? superUsersFieldKey, string? branchFieldKey = null, string? featuresFieldKey = null, string? ministryFieldKey = null, string? programAreaFieldKey = null);
    Task CreateTenantAsync(Guid id, CreateTenantInputDto? input);
    Task<OnboardingColumnSchemaDto> GetColumnSchemaAsync(string? category = null);
    Task<List<string>> GetAvailableCategoriesAsync();
}
