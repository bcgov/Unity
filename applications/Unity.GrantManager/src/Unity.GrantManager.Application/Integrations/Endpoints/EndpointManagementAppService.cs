using Microsoft.Extensions.Caching.Distributed;
using StackExchange.Redis;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace Unity.GrantManager.Integrations.Endpoints
{
    public class EndpointManagementAppService :
        CrudAppService<
            DynamicUrl,
            DynamicUrlDto,
            Guid,
            PagedAndSortedResultRequestDto,
            CreateUpdateDynamicUrlDto>,
        IEndpointManagementAppService
    {
        private readonly IDistributedCache _cache;
        private readonly IConnectionMultiplexer _redis; // for Redis-specific ops

        public EndpointManagementAppService(
            IRepository<DynamicUrl, Guid> repository,
            IDistributedCache cache,
            IConnectionMultiplexer redis) : base(repository)
        {
            _cache = cache;
            _redis = redis;
        }

        private static string BuildCacheKey(string keyName, bool tenantSpecific, Guid? tenantId)
            => $"DynamicUrl:{tenantSpecific}:{tenantId ?? Guid.Empty}:{keyName}";

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
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) // configurable
                    });
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
        }

        /// <summary>
        /// Clears all DynamicUrl cache entries from Redis efficiently using SCAN.
        /// If tenantId is specified, only clears that tenant's entries.
        /// </summary>
        public async Task ClearCacheAsync(Guid? tenantId = null)
        {
            var server = _redis.GetServer(_redis.GetEndPoints()[0]);
            var pattern = tenantId == null
                ? "DynamicUrl:*" // all tenants
                : $"DynamicUrl:*:{tenantId}:*"; // scoped to one tenant

            foreach (var key in server.Keys(pattern: pattern))
            {
                await _redis.GetDatabase().KeyDeleteAsync(key);
            }
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
