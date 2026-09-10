using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Flex.Scoresheets.Events;

namespace Unity.GrantManager.Assessments
{
    /// <summary>
    /// Scoresheet-related concerns of an Assessment, extracted out of <see cref="AssessmentAppService"/>.
    /// Internal collaborator, not exposed as a public API.
    /// </summary>
    public interface IAssessmentScoresheetService
    {
        Task<SubTotalDto> GetSubTotalAsync(AssessmentListItemDto assessment);

        Task<bool> IsScoresheetNotLinkedToFormAsync(Guid applicationFormId);

        Task ValidateAnswersOnCompleteAsync(Guid assessmentId, AssessmentAction triggerAction);

        Task CopyAiAnswersIfEnabledAsync(Guid applicationId, Guid newAssessmentId);

        Task PersistSectionAnswersIfEnabledAsync(Guid assessmentId, List<AssessmentAnswersEto> answers);
    }
}
