using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Domain.Repositories;

namespace Unity.GrantManager.Applications
{
    public interface IApplicationChefsFileAttachmentRepository : IRepository<ApplicationChefsFileAttachment, Guid>
    {
        Task<List<ApplicationChefsFileAttachment>> GetListAsync(Guid applicationId);
    }
}
