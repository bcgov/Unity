using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Applications
{
    public interface IApplicationRepository : IRepository<Application, Guid>
    {
        // Basic single application fetch
        Task<Application> WithBasicDetailsAsync(Guid id);
        Task<Application?> GetWithFullDetailsByIdAsync(Guid id);

        // Fetch multiple applications by IDs
        Task<List<Application>> GetListByIdsAsync(Guid[] ids);

        // Count with optional submitted date filters
        Task<long> GetCountAsync(DateTime? submittedFromDate, DateTime? submittedToDate);

        // Optimized list with filtering, paging, sorting
        Task<List<Application>> WithFullDetailsAsync(
            int skipCount,
            int maxResultCount,
            string? sorting = null,
            DateTime? submittedFromDate = null,
            DateTime? submittedToDate = null,
            string? searchTerm = null // optional search filter
        );
    }
}
