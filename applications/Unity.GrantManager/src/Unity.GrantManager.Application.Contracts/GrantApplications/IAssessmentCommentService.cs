using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using Volo.Abp.Application.Services;

namespace Unity.GrantManager.GrantApplications
{
    public interface IAssessmentCommentService : IApplicationService
    {
        Task<IList<AssessmentCommentDto>> GetListAsync(Guid applicationFormSubmissionId);
    }
}