using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Applications
{
    public interface IAssessmentAttachmentService : IApplicationService
    {        
        Task<IList<AssessmentAttachmentDto>> GetListAsync(Guid assessmentId);

    }
}
