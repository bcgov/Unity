using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Applications
{
    [Dependency(ReplaceServices = true)]
    [ExposeServices(typeof(ApplicationBulkActionsAppService), typeof(IApplicationBulkActionsAppService))]
    [Authorize]
    public class ApplicationBulkActionsAppService : GrantManagerAppService, IApplicationBulkActionsAppService
    {
        private readonly ApplicationIdsCacheService _cacheService;

        public ApplicationBulkActionsAppService(ApplicationIdsCacheService cacheService)
        {
            _cacheService = cacheService;
        }

        /// <summary>
        /// Stores application IDs in distributed cache for bulk operations
        /// </summary>
        /// <param name="input">Request containing list of application IDs</param>
        /// <returns>Cache key to retrieve the stored IDs</returns>
        public async Task<StoreApplicationIdsResultDto> StoreApplicationIdsAsync(StoreApplicationIdsRequestDto input)
        {
            if (input == null || input.ApplicationIds == null || input.ApplicationIds.Count == 0)
            {
                throw new UserFriendlyException("No application IDs provided");
            }

            try
            {
                var cacheKey = await _cacheService.StoreApplicationIdsAsync(input.ApplicationIds);

                Logger.LogInformation(
                    "User {UserId} stored {Count} application IDs for bulk operation with cache key: {CacheKey}",
                    CurrentUser?.Id,
                    input.ApplicationIds.Count,
                    cacheKey);

                return new StoreApplicationIdsResultDto
                {
                    CacheKey = cacheKey
                };
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to store application IDs for bulk operation");
                throw new UserFriendlyException("Failed to prepare bulk operation. Please try again.");
            }
        }
    }
}
