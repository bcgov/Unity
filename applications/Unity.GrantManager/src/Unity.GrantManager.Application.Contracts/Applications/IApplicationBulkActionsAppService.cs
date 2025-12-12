using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Applications
{
    public interface IApplicationBulkActionsAppService : IApplicationService
    {
        /// <summary>
        /// Stores application IDs in distributed cache for bulk operations
        /// </summary>
        /// <param name="input">Request containing list of application IDs</param>
        /// <returns>Cache key to retrieve the stored IDs</returns>
        Task<StoreApplicationIdsResultDto> StoreApplicationIdsAsync(StoreApplicationIdsRequestDto input);
    }

    public class StoreApplicationIdsRequestDto
    {
        public List<Guid> ApplicationIds { get; set; } = new List<Guid>();
    }

    public class StoreApplicationIdsResultDto
    {
        public string CacheKey { get; set; } = string.Empty;
    }
}
