using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Volo.Abp.Application.Services;
using Unity.GrantManager.Comments;

namespace Unity.GrantManager.Assessments;

public interface IAssessmentAppService : IApplicationService, ICommentsService
{
    Task<AssessmentDto> CreateAsync(CreateAssessmentDto dto);
    Task<IList<AssessmentDto>> GetListAsync(Guid applicationId);
}
