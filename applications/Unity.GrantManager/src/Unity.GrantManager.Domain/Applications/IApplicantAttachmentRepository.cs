using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Applications
{
    public interface IApplicantAttachmentRepository : IRepository<ApplicantAttachment, Guid>
    {
        Task<List<ApplicantAttachment>> GetListAsync(
            int skipCount,
            int maxResultCount,
            string sorting,
            string filter
        );
    }
}
