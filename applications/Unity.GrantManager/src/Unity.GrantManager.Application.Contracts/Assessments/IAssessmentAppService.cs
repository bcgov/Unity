using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Assessments;

public interface IAssessmentAppService : IApplicationService
{
    Task<AssessmentDto> CreateAsync(CreateAssessmentDto dto);
    Task<IList<AssessmentDto>> GetListAsync(Guid applicationId);
    Task UpdateAssessmentRecommendation(UpdateAssessmentRecommendationDto dto);
    List<AssessmentAction> GetAllActions();
    Task<List<AssessmentAction>> GetPermittedActions(Guid assessmentId);
    Task<AssessmentDto> ExecuteAssessmentAction(Guid assessmentId, AssessmentAction triggerAction);
    Task<Guid?> GetCurrentUserAssessmentId(Guid applicationId);
    Task UpdateAssessmentScore(AssessmentScoresDto dto);    
    Task SaveScoresheetSectionAnswers(AssessmentScoreSectionDto dto);
}
