using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Applications;

public interface IAssessmentAttachmentRepository : IRepository<AssessmentAttachment, Guid>
{
    Task<List<AssessmentAttachment>> GetListAsync(
            int skipCount,
            int maxResultCount,
            string sorting,
            string filter
        );
}
