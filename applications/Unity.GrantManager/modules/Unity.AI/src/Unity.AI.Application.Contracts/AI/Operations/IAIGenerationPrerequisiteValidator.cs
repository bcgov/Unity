using System;
using System.Threading.Tasks;

namespace Unity.AI.Operations;

public interface IAIGenerationPrerequisiteValidator
{
    Task EnsureAttachmentSummaryAvailableAsync(Guid applicationId);

    Task EnsureApplicationAnalysisAvailableAsync(Guid applicationId);

    Task EnsureApplicationScoringAvailableAsync(Guid applicationId);

    Task EnsureFormMappingAvailableAsync(Guid applicationFormVersionId);
}
