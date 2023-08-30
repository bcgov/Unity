using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Applications
{
    public interface IApplicationCommentsRepository : IRepository<ApplicationComment, Guid>
    {
        Task<List<ApplicationComment>> GetListAsync(
            int skipCount,
            int maxResultCount,
            string sorting,
            string filter
        );
    }
}
