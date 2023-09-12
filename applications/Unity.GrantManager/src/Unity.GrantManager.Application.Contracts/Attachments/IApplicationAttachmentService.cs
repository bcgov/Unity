using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Applications
{
    public interface IApplicationAttachmentService : IApplicationService
    {        
        Task<IList<ApplicationAttachmentDto>> GetListAsync(Guid applicationId);

    }
}
