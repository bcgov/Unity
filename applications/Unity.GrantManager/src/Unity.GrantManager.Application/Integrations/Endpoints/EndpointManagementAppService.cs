using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

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
        // Key: (keyName, tenantSpecific, tenantId), Value: url
        private static readonly ConcurrentDictionary<(string keyName, bool tenantSpecific, Guid? tenantId), string> _urlCache
            = new();

        private string? _chefsApiBaseUrl; // Lazy initialized

        [UnitOfWork]
        public async Task<string> GetChefsApiBaseUrlAsync()
        {
            if (_chefsApiBaseUrl == null)
            {
                _chefsApiBaseUrl = await GetUrlByKeyNameInternalAsync(DynamicUrlKeyNames.INTAKE_API_BASE, tenantSpecific: false);

                if (string.IsNullOrWhiteSpace(_chefsApiBaseUrl))
                    throw new UserFriendlyException("CHEFS API base URL not configured.");
            }

            return _chefsApiBaseUrl;
        }

        [UnitOfWork]
        public Task<string> GetUgmUrlByKeyNameAsync(string keyName)
        {
            return GetUrlByKeyNameInternalAsync(keyName, tenantSpecific: false);
        }

        public Task<string> GetUrlByKeyNameAsync(string keyName)
        {
            return GetUrlByKeyNameInternalAsync(keyName, tenantSpecific: true);
        }

        private async Task<string> GetUrlByKeyNameInternalAsync(string keyName, bool tenantSpecific)
        {
            Guid? tenantId = tenantSpecific ? CurrentTenant.Id : null;
            var cacheKey = (keyName, tenantSpecific, tenantId);

            // O(1) cache lookup
            if (_urlCache.TryGetValue(cacheKey, out var cachedUrl))
            {
                return cachedUrl;
            }

            // Cache miss: fetch from DB
            DynamicUrl? dynamicUrl;
            if (tenantSpecific)
            {
                dynamicUrl = await Repository.FirstOrDefaultAsync(x => x.KeyName == keyName);
            }
            else
            {
                using (CurrentTenant.Change(null))
                {
                    dynamicUrl = await Repository.FirstOrDefaultAsync(x => x.KeyName == keyName && x.TenantId == null);
                }
            }

            var url = dynamicUrl?.Url ?? string.Empty;

            // Cache only if not empty
            if (!string.IsNullOrWhiteSpace(url))
            {
                _urlCache[cacheKey] = url;
            }

            return url;
        }

        // Optional: explicit cache invalidation for a key
        public static void InvalidateCache(string keyName, bool tenantSpecific, Guid? tenantId)
        {
            var cacheKey = (keyName, tenantSpecific, tenantId);
            _urlCache.TryRemove(cacheKey, out _);
        }

        // Optional: clear entire cache
        public void ClearCache()
        {
            _urlCache.Clear();
        }
    }
}
