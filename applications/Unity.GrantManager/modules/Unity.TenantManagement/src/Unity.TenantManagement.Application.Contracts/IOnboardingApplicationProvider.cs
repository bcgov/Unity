#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace Unity.TenantManagement;

public interface IOnboardingApplicationProvider
{
    Task<PagedResultDto<OnboardingApplicationRecord>> GetPagedListAsync(
        int skipCount,
        int maxResultCount,
        string? sorting,
        string category,
        string? filter = null,
        IReadOnlyList<Guid>? globalDynamicMatchIds = null,
        IReadOnlyList<ColumnFilterDto>? staticColumnFilters = null,
        IReadOnlyList<Guid>? dynamicColumnMatchIds = null);
    Task<OnboardingApplicationRecord?> GetByIdAsync(Guid id);
    Task<List<Guid>> GetAllIdsAsync(string category);
    Task<List<Guid>> GetFormVersionIdsAsync(string category);
    Task<List<OnboardingColumnDto>> GetMappedCoreFieldColumnsAsync(string category);
    Task<List<string>> GetAvailableCategoriesAsync();
    Task CloseApplicationAsync(Guid applicationId);
}
