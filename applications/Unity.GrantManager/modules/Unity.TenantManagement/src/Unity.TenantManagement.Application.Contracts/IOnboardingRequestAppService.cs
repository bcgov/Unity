#nullable enable
using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Unity.TenantManagement;

public interface IOnboardingRequestAppService : IApplicationService
{
    Task<PagedResultDto<OnboardingRequestDto>> GetListAsync(PagedAndSortedResultRequestDto input);
    Task<OnboardingRequestDto?> GetAsync(Guid id);
    Task<OnboardingValidationResultDto> ValidateAsync(Guid id);
    Task CreateTenantAsync(Guid id);
}
