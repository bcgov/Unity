using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Applications
{
    public interface IApplicationAttachmentRepository : IRepository<ApplicationAttachment, Guid>
    {
        Task<List<ApplicationAttachment>> GetListAsync(
            int skipCount,
            int maxResultCount,
            string sorting,
            string filter
        );
    }
}
