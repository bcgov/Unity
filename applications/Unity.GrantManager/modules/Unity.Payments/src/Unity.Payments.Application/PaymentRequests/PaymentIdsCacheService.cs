using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace Unity.Payments.PaymentRequests
{
    public class PaymentIdsCacheService : ITransientDependency
    {
        private readonly IDistributedCache<List<Guid>, string> _cache;
        private readonly ILogger<PaymentIdsCacheService> _logger;
        private const string CACHE_KEY_PREFIX = "BulkAction:PaymentRequestIds:";
        private const int CACHE_EXPIRATION_MINUTES = 5;

        public PaymentIdsCacheService(
            IDistributedCache<List<Guid>, string> cache,
            ILogger<PaymentIdsCacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Stores payment request IDs in distributed cache and returns a unique cache key
        /// </summary>
        /// <param name="paymentRequestIds">List of payment request IDs to store</param>
        /// <returns>Unique cache key to retrieve the data</returns>
        public async Task<string> StorePaymentIdsAsync(List<Guid> paymentRequestIds)
        {
            if (paymentRequestIds == null || paymentRequestIds.Count == 0)
            {
                throw new ArgumentException("Payment request IDs list cannot be null or empty", nameof(paymentRequestIds));
            }

            var cacheKey = GenerateCacheKey();

            try
            {
                await _cache.SetAsync(
                    cacheKey,
                    paymentRequestIds,
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CACHE_EXPIRATION_MINUTES)
                    });

                _logger.LogInformation(
                    "Stored {Count} payment request IDs in cache with key: {CacheKey}",
                    paymentRequestIds.Count,
                    cacheKey);

                return cacheKey;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store payment request IDs in cache");
                throw new InvalidOperationException(
                    $"Failed to store {paymentRequestIds.Count} payment request IDs in distributed cache with key {cacheKey}",
                    ex);
            }
        }

        /// <summary>
        /// Retrieves payment request IDs from distributed cache using the cache key
        /// </summary>
        /// <param name="cacheKey">Cache key returned from StorePaymentIdsAsync</param>
        /// <returns>List of payment request IDs, or null if not found/expired</returns>
        public async Task<List<Guid>?> GetPaymentIdsAsync(string cacheKey)
        {
            if (string.IsNullOrWhiteSpace(cacheKey))
            {
                _logger.LogWarning("Cache key is null or empty");
                return null;
            }

            try
            {
                var paymentRequestIds = await _cache.GetAsync(cacheKey);

                if (paymentRequestIds == null)
                {
                    _logger.LogWarning("No data found for cache key: {CacheKey} (expired or invalid)", cacheKey);
                }
                else
                {
                    _logger.LogInformation(
                        "Retrieved {Count} payment request IDs from cache with key: {CacheKey}",
                        paymentRequestIds.Count,
                        cacheKey);
                }

                return paymentRequestIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve payment request IDs from cache with key: {CacheKey}", cacheKey);
                return null;
            }
        }

        /// <summary>
        /// Removes payment request IDs from distributed cache
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
        /// Generates a unique cache key for storing payment request IDs
        /// </summary>
        /// <returns>Unique cache key with prefix</returns>
        private static string GenerateCacheKey()
        {
            return $"{CACHE_KEY_PREFIX}{Guid.NewGuid()}";
        }
    }
}
