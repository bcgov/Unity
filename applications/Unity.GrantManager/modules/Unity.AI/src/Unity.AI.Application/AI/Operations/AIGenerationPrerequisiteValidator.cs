using Microsoft.Extensions.Localization;
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Domain.Scoresheets;
using Unity.Flex.Domain.Worksheets;
using Unity.AI.Localization;
using Unity.GrantManager.Applications;
using Unity.Modules.Shared.Correlation;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Linq;

namespace Unity.AI.Operations;

public class AIGenerationPrerequisiteValidator(
    IApplicationRepository applicationRepository,
    IApplicationFormRepository applicationFormRepository,
    IApplicationFormVersionRepository applicationFormVersionRepository,
    IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
    IApplicationChefsFileAttachmentRepository applicationChefsFileAttachmentRepository,
    IScoresheetRepository scoresheetRepository,
    IWorksheetListRepository worksheetListRepository,
    IAsyncQueryableExecuter asyncExecuter,
    IStringLocalizer<AIResource> localizer) : IAIGenerationPrerequisiteValidator, ITransientDependency
{
    public async Task EnsureAttachmentSummaryAvailableAsync(Guid applicationId)
    {
        var attachmentQuery = await applicationChefsFileAttachmentRepository.GetQueryableAsync();
        var hasAttachments = await asyncExecuter.AnyAsync(attachmentQuery.Where(a => a.ApplicationId == applicationId));
        if (!hasAttachments)
        {
            throw new UserFriendlyException(localizer[AILocalizationKeys.NoAttachmentsAvailable]);
        }
    }

    public async Task EnsureApplicationAnalysisAvailableAsync(Guid applicationId)
    {
        var submission = await applicationFormSubmissionRepository.GetByApplicationAsync(applicationId);
        if (submission == null || string.IsNullOrWhiteSpace(submission.Submission))
        {
            throw new UserFriendlyException(localizer[AILocalizationKeys.ApplicationAnalysisRequiresSubmission]);
        }
    }

    public async Task EnsureApplicationScoringAvailableAsync(Guid applicationId)
    {
        var application = await applicationRepository.GetAsync(applicationId);
        var applicationForm = await applicationFormRepository.GetAsync(application.ApplicationFormId);
        if (applicationForm.ScoresheetId == null)
        {
            throw new UserFriendlyException(localizer[AILocalizationKeys.ScoringRequiresScoresheet]);
        }

        var scoresheet = await scoresheetRepository.GetWithChildrenAsync(applicationForm.ScoresheetId.Value);
        if (scoresheet == null || !scoresheet.Sections.Any() || !scoresheet.Sections.SelectMany(s => s.Fields).Any())
        {
            throw new UserFriendlyException(localizer[AILocalizationKeys.ScoringRequiresScoresheetFields]);
        }
    }

    public async Task EnsureFormMappingAvailableAsync(Guid applicationFormVersionId)
    {
        var formVersion = await applicationFormVersionRepository.FindAsync(applicationFormVersionId);
        if (formVersion == null)
        {
            throw new UserFriendlyException(localizer[AILocalizationKeys.FormMappingRequiresFormVersion]);
        }
    }

    public async Task EnsureFormWorksheetAvailableAsync(Guid applicationFormVersionId)
    {
        var formVersion = await applicationFormVersionRepository.FindAsync(applicationFormVersionId);
        if (formVersion == null)
        {
            throw new UserFriendlyException(localizer[AILocalizationKeys.FormWorksheetRequiresFormVersion]);
        }

        var worksheetLinks = await worksheetListRepository.GetListByCorrelationAsync(
            formVersion.Id,
            CorrelationConsts.FormVersion,
            true);

        if (worksheetLinks == null)
        {
            throw new UserFriendlyException(localizer[AILocalizationKeys.FormWorksheetRequiresFormVersion]);
        }
    }

    public async Task EnsureFormScoresheetAvailableAsync(Guid applicationFormVersionId)
    {
        var formVersion = await applicationFormVersionRepository.FindAsync(applicationFormVersionId);
        if (formVersion == null)
        {
            throw new UserFriendlyException(localizer[AILocalizationKeys.FormScoresheetRequiresFormVersion]);
        }

        var applicationForm = await applicationFormRepository.GetAsync(formVersion.ApplicationFormId);
        if (applicationForm.ScoresheetId == null)
        {
            throw new UserFriendlyException(localizer[AILocalizationKeys.FormScoresheetRequiresFormVersion]);
        }

        var scoresheet = await scoresheetRepository.GetWithChildrenAsync(applicationForm.ScoresheetId.Value);
        if (scoresheet == null || !scoresheet.Sections.Any() || !scoresheet.Sections.SelectMany(s => s.Fields).Any())
        {
            throw new UserFriendlyException(localizer[AILocalizationKeys.FormScoresheetRequiresFormVersion]);
        }
    }
}
