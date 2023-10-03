using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.Assessments;

public interface IAssessmentAppService : IApplicationService
{
    Task<AssessmentDto> CreateAssessment(CreateAssessmentDto dto);
    Task UpdateAssessmentRecommendation(UpdateAssessmentRecommendationDto dto);
    Task<IList<AssessmentDto>> GetListAsync(Guid applicationId);
    Task<List<AssessmentAction>> GetAvailableActions(Guid assessmentId);
    List<string?> GetAllActions();
    Task<AssessmentDto> ExecuteAssessmentAction(Guid assessmentId, AssessmentAction triggerAction = AssessmentAction.SendToTeamLead);
    Task<Guid?> GetCurrentUserAssessmentId(Guid applicationId);
}
