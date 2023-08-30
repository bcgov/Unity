using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Assessments
{
    public interface IAssessmentCommentsService : IApplicationService
    {
        Task<AssessmentCommentDto> CreateAssessmentComment(CreateAssessmentCommentDto dto);
        Task UpdateAssessmentComment(UpdateAssessmentCommentDto dto);
        Task<IList<AssessmentCommentDto>> GetListAsync(Guid assessmentId);        
    }
}