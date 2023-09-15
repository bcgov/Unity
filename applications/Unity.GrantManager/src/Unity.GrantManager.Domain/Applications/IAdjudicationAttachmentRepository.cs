using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Applications;

public interface IAdjudicationAttachmentRepository : IRepository<AdjudicationAttachment, Guid>
{
    Task<List<AdjudicationAttachment>> GetListAsync(
            int skipCount,
            int maxResultCount,
            string sorting,
            string filter
        );
}
