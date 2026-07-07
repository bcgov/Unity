using Microsoft.Extensions.Localization;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Domain.Scoresheets;
using Unity.AI.Localization;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace Unity.AI.Operations;

public class AIGenerationPrerequisiteValidator(
    IAIApplicationInputDataProvider dataProvider,
    IStringLocalizer<AIResource> localizer) : IAIGenerationPrerequisiteValidator, ITransientDependency
{
    public async Task EnsureAttachmentSummaryAvailableAsync(Guid applicationId)
    {
        if (!await dataProvider.HasAttachmentsAsync(applicationId))
        {
            throw new UserFriendlyException(localizer[AILocalizationKeys.NoAttachmentsAvailable]);
        }
    }

    public async Task EnsureApplicationAnalysisAvailableAsync(Guid applicationId)
    {
        if (!await dataProvider.HasSubmissionAsync(applicationId))
        {
            throw new UserFriendlyException(localizer[AILocalizationKeys.ApplicationAnalysisRequiresSubmission]);
        }
    }

    public async Task EnsureApplicationScoringAvailableAsync(Guid applicationId)
    {
        var applicationForm = await dataProvider.GetApplicationFormAsync(applicationId);
        if (applicationForm?.ScoresheetId == null)
        {
            throw new UserFriendlyException(localizer[AILocalizationKeys.ScoringRequiresScoresheet]);
        }

        var scoresheet = await dataProvider.GetScoresheetAsync(applicationForm.ScoresheetId.Value);
        if (scoresheet == null || !scoresheet.Sections.Any() || !scoresheet.Sections.SelectMany(s => s.Fields).Any())
        {
            throw new UserFriendlyException(localizer[AILocalizationKeys.ScoringRequiresScoresheetFields]);
        }
    }
}
