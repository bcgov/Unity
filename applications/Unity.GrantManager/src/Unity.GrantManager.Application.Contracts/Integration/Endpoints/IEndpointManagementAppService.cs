using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Integrations;


public interface IEndpointManagementAppService : ICrudAppService<
            DynamicUrlDto,
            Guid,
            PagedAndSortedResultRequestDto,
            CreateUpdateDynamicUrlDto>

{
    Task<string> GetChefsApiBaseUrlAsync();
    Task<string> GetUrlByKeyNameAsync(string keyName);
    Task<string> GetUgmUrlByKeyNameAsync(string keyName);
    Task ClearCacheAsync(Guid? tenantId = null);
}
