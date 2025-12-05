using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace Unity.Payments.PaymentRequests
{
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(PaymentBulkActionsAppService), typeof(IPaymentBulkActionsAppService))]
    [Authorize]
    public class PaymentBulkActionsAppService : PaymentsAppService, IPaymentBulkActionsAppService
    {
        private readonly PaymentIdsCacheService _cacheService;

        public PaymentBulkActionsAppService(PaymentIdsCacheService cacheService)
        {
            _cacheService = cacheService;
        }

        /// <summary>
        /// Stores payment request IDs in distributed cache for bulk operations
        /// </summary>
        /// <param name="input">Request containing list of payment request IDs</param>
        /// <returns>Cache key to retrieve the stored IDs</returns>
        public async Task<StorePaymentIdsResultDto> StorePaymentIdsAsync(StorePaymentIdsRequestDto input)
        {
            if (input == null || input.PaymentRequestIds == null || input.PaymentRequestIds.Count == 0)
            {
                throw new UserFriendlyException("No payment request IDs provided");
            }

            try
            {
                var cacheKey = await _cacheService.StorePaymentIdsAsync(input.PaymentRequestIds);

                Logger.LogInformation(
                    "User {UserId} stored {Count} payment request IDs for bulk operation with cache key: {CacheKey}",
                    CurrentUser?.Id,
                    input.PaymentRequestIds.Count,
                    cacheKey);

                return new StorePaymentIdsResultDto
                {
                    CacheKey = cacheKey
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to store payment request IDs for bulk operation");
                throw new UserFriendlyException("Failed to prepare bulk operation. Please try again.");
            }
        }
    }
}
