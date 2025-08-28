using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace Unity.GrantManager.Integrations.Endpoints
{
    public class EndpointManagementAppService(
        IRepository<DynamicUrl, Guid> repository,
        IDistributedCache cache) :
        CrudAppService<
            DynamicUrl,
            DynamicUrlDto,
            Guid,
            PagedAndSortedResultRequestDto,
            CreateUpdateDynamicUrlDto>(repository),
        IEndpointManagementAppService
    {
        private readonly IDistributedCache _cache = cache;
        private const string CACHE_KEY_SET_PREFIX = "DynamicUrl:KeySet";

        private static string BuildCacheKey(string keyName, bool tenantSpecific, Guid? tenantId)
            => $"DynamicUrl:{tenantSpecific}:{tenantId ?? Guid.Empty}:{keyName}";

        private static string BuildCacheKeySetKey(Guid? tenantId)
            => $"{CACHE_KEY_SET_PREFIX}:{tenantId ?? Guid.Empty}";

        private async Task AddToKeySetAsync(string cacheKey, Guid? tenantId)
        {
            var keySetKey = BuildCacheKeySetKey(tenantId);
            var existing = await _cache.GetStringAsync(keySetKey);
            var keySet = string.IsNullOrEmpty(existing)
                ? new HashSet<string>()
                : System.Text.Json.JsonSerializer.Deserialize<HashSet<string>>(existing) ?? new HashSet<string>();

            keySet.Add(cacheKey);
            var serialized = System.Text.Json.JsonSerializer.Serialize(keySet);

            await _cache.SetStringAsync(keySetKey, serialized, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24) // longer than individual entries
            });
        }

        private async Task RemoveFromKeySetAsync(string cacheKey, Guid? tenantId)
        {
            var keySetKey = BuildCacheKeySetKey(tenantId);
            var existing = await _cache.GetStringAsync(keySetKey);
            if (string.IsNullOrEmpty(existing)) return;

            var keySet = System.Text.Json.JsonSerializer.Deserialize<HashSet<string>>(existing);
            if (keySet?.Remove(cacheKey) == true)
            {
                var serialized = System.Text.Json.JsonSerializer.Serialize(keySet);
                await _cache.SetStringAsync(keySetKey, serialized, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
                });
            }
        }

        [UnitOfWork]
        public async Task<string> GetChefsApiBaseUrlAsync()
        {
            var url = await GetUrlByKeyNameInternalAsync(DynamicUrlKeyNames.INTAKE_API_BASE, tenantSpecific: false);

            if (string.IsNullOrWhiteSpace(url))
                throw new UserFriendlyException("CHEFS API base URL not configured.");

            return url!;
        }

        [UnitOfWork]
        public async Task<string> GetUgmUrlByKeyNameAsync(string keyName)
        {
            var url = await GetUrlByKeyNameInternalAsync(keyName, tenantSpecific: false);
            if (url == null)
                throw new UserFriendlyException($"URL for key '{keyName}' not configured.");
            return url;
        }

        public async Task<string> GetUrlByKeyNameAsync(string keyName)
        {
            var url = await GetUrlByKeyNameInternalAsync(keyName, tenantSpecific: true);
            if (url == null)
                throw new UserFriendlyException($"URL for key '{keyName}' not configured.");
            return url;
        }

        private async Task<string?> GetUrlByKeyNameInternalAsync(string keyName, bool tenantSpecific)
        {
            var tenantId = tenantSpecific ? CurrentTenant.Id : null;
            var cacheKey = BuildCacheKey(keyName, tenantSpecific, tenantId);

            // try cache first
            var cached = await _cache.GetStringAsync(cacheKey);
            if (cached != null)
                return cached;

            DynamicUrl? dynamicUrl;
            if (tenantSpecific)
            {
                dynamicUrl = await Repository.FirstOrDefaultAsync(x => x.KeyName == keyName && x.TenantId == tenantId);
            }
            else
            {
                using (CurrentTenant.Change(null))
                {
                    dynamicUrl = await Repository.FirstOrDefaultAsync(x => x.KeyName == keyName && x.TenantId == null);
                }
            }

            var url = dynamicUrl?.Url;

            if (!string.IsNullOrWhiteSpace(url))
            {
                await _cache.SetStringAsync(
                    cacheKey,
                    url,
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
                    });

                // Track this key
                await AddToKeySetAsync(cacheKey, tenantId);
            }

            return url;
        }

        // ------------------------------
        // Cache invalidation methods
        // ------------------------------
        public async Task InvalidateCacheAsync(string keyName, bool tenantSpecific, Guid? tenantId)
        {
            var cacheKey = BuildCacheKey(keyName, tenantSpecific, tenantId);
            await _cache.RemoveAsync(cacheKey);
            await RemoveFromKeySetAsync(cacheKey, tenantId);
        }

        /// <summary>
        /// Clears all DynamicUrl cache entries for the specified tenant (or all tenants if null).
        /// Uses tracked cache keys for efficient bulk clearing.
        /// </summary>
        public async Task ClearCacheAsync(Guid? tenantId = null)
        {
            var keySetKey = BuildCacheKeySetKey(tenantId);
            var existing = await _cache.GetStringAsync(keySetKey);

            if (!string.IsNullOrEmpty(existing))
            {
                var keySet = System.Text.Json.JsonSerializer.Deserialize<HashSet<string>>(existing);
                if (keySet != null)
                {
                    // Remove all tracked cache entries
                    var tasks = keySet.Select(key => _cache.RemoveAsync(key));
                    await Task.WhenAll(tasks);
                }
            }

            // Clear the key set itself
            await _cache.RemoveAsync(keySetKey);
        }

        // ------------------------------
        // CRUD overrides for invalidation
        // ------------------------------
        public override async Task<DynamicUrlDto> CreateAsync(CreateUpdateDynamicUrlDto input)
        {
            var result = await base.CreateAsync(input);
            await InvalidateCacheAsync(result.KeyName, result.TenantId != Guid.Empty, result.TenantId);
            return result;
        }

        public override async Task<DynamicUrlDto> UpdateAsync(Guid id, CreateUpdateDynamicUrlDto input)
        {
            var result = await base.UpdateAsync(id, input);
            await InvalidateCacheAsync(result.KeyName, result.TenantId != Guid.Empty, result.TenantId);
            return result;
        }

        public override async Task DeleteAsync(Guid id)
        {
            var entity = await Repository.GetAsync(id);
            await base.DeleteAsync(id);
            await InvalidateCacheAsync(entity.KeyName, entity.TenantId != Guid.Empty, entity.TenantId);
        }
    }
}