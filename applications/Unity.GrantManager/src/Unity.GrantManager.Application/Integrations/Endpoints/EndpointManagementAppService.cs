using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Integrations.Endpoints
{
    [ExposeServices(typeof(EndpointManagementAppService), typeof(IEndpointManagementAppService))]
    public class EndpointManagementAppService(IRepository<DynamicUrl, Guid> repository) :
            CrudAppService<
            DynamicUrl,
            DynamicUrlDto,
            Guid,
            PagedAndSortedResultRequestDto,
            CreateUpdateDynamicUrlDto>(repository),
            IEndpointManagementAppService
    {
        public async Task<string> GetChefsApiBaseUrlAsync()
        {
            var url = await GetUrlByKeyNameAsync(DynamicUrlKeyNames.INTAKE_API_BASE);
            if (string.IsNullOrWhiteSpace(url)) throw new Exception("CHEFS API base URL not configured.");
            return url;
        }


        public async Task<string> GetUrlByKeyNameAsync(string keyName)
        {
            var dynamicUrl = await Repository.FirstOrDefaultAsync(x => x.KeyName == keyName);
            return dynamicUrl?.Url ?? string.Empty;
        }
    }
}
