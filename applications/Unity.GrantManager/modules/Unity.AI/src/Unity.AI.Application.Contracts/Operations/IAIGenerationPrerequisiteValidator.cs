using System;
using System.Threading.Tasks;
using Unity.AI.Generation;

namespace Unity.AI.Operations;

public interface IAIGenerationPrerequisiteValidator
{
    Task EnsureAvailableAsync(string operationType, AIGenerationSubmissionDto request);

    Task EnsureAttachmentSummaryAvailableAsync(Guid applicationId);

    Task EnsureApplicationAnalysisAvailableAsync(Guid applicationId);

    Task EnsureApplicationScoringAvailableAsync(Guid applicationId);

    Task EnsureFormMappingAvailableAsync(Guid applicationFormVersionId);

    Task EnsureFormWorksheetAvailableAsync(Guid applicationFormVersionId);

    Task EnsureFormScoresheetAvailableAsync(Guid applicationFormVersionId);
}
