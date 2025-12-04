using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Applications
{
    public class ApplicationIdsCacheService : ITransientDependency
    {
        private readonly IDistributedCache<List<Guid>, string> _cache;
        private readonly ILogger<ApplicationIdsCacheService> _logger;
        private const string CACHE_KEY_PREFIX = "BulkAction:ApplicationIds:";
        private const int CACHE_EXPIRATION_MINUTES = 5;

        public ApplicationIdsCacheService(
            IDistributedCache<List<Guid>, string> cache,
            ILogger<ApplicationIdsCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Stores application IDs in distributed cache and returns a unique cache key
        /// </summary>
        /// <param name="applicationIds">List of application IDs to store</param>
        /// <returns>Unique cache key to retrieve the data</returns>
        public async Task<string> StoreApplicationIdsAsync(List<Guid> applicationIds)
        {
            if (applicationIds == null || applicationIds.Count == 0)
            {
                throw new ArgumentException("Application IDs list cannot be null or empty", nameof(applicationIds));
            }

            var cacheKey = GenerateCacheKey();

            try
            {
                await _cache.SetAsync(
                    cacheKey,
                    applicationIds,
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_EXPIRATION_MINUTES)
                    });

                _logger.LogInformation(
                    "Stored {Count} application IDs in cache with key: {CacheKey}",
                    applicationIds.Count,
                    cacheKey);

                return cacheKey;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store application IDs in cache");
                throw new InvalidOperationException(
                    $"Failed to store {applicationIds.Count} application IDs in distributed cache with key {cacheKey}",
                    ex);
            }
        }

        /// <summary>
        /// Retrieves application IDs from distributed cache using the cache key
        /// </summary>
        /// <param name="cacheKey">Cache key returned from StoreApplicationIdsAsync</param>
        /// <returns>List of application IDs, or null if not found/expired</returns>
        public async Task<List<Guid>?> GetApplicationIdsAsync(string cacheKey)
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
            {
                _logger.LogWarning("Cache key is null or empty");
                return null;
            }

            try
            {
                var applicationIds = await _cache.GetAsync(cacheKey);

                if (applicationIds == null)
                {
                    _logger.LogWarning("No data found for cache key: {CacheKey} (expired or invalid)", cacheKey);
                }
                else
                {
                    _logger.LogInformation(
                        "Retrieved {Count} application IDs from cache with key: {CacheKey}",
                        applicationIds.Count,
                        cacheKey);
                }

                return applicationIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve application IDs from cache with key: {CacheKey}", cacheKey);
                return null;
            }
        }

        /// <summary>
        /// Removes application IDs from distributed cache
        /// </summary>
        /// <param name="cacheKey">Cache key to remove</param>
        public async Task RemoveAsync(string cacheKey)
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
            {
                return;
            }

            try
            {
                await _cache.RemoveAsync(cacheKey);
                _logger.LogInformation("Removed cache entry with key: {CacheKey}", cacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove cache entry with key: {CacheKey}", cacheKey);
                // Don't throw - cache removal failure is not critical
            }
        }

        /// <summary>
        /// Generates a unique cache key for storing application IDs
        /// </summary>
        /// <returns>Unique cache key with prefix</returns>
        private static string GenerateCacheKey()
        {
            return $"{CACHE_KEY_PREFIX}{Guid.NewGuid()}";
        }
    }
}
