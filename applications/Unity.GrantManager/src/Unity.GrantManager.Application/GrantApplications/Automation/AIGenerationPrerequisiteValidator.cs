using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.AI;
using Unity.AI.Generation;
using Unity.AI.Operations;
using Unity.Flex.Domain.Scoresheets;
using Unity.GrantManager.Applications;
using Unity.GrantManager.ApplicationForms;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Linq;

namespace Unity.GrantManager.GrantApplications.Automation;

public class AIGenerationPrerequisiteValidator(
    IApplicationRepository applicationRepository,
    IApplicationFormRepository applicationFormRepository,
    IApplicationFormVersionRepository applicationFormVersionRepository,
    IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
    IApplicationChefsFileAttachmentRepository applicationChefsFileAttachmentRepository,
    IScoresheetRepository scoresheetRepository,
    IAsyncQueryableExecuter asyncExecuter,
    IGenerationReviewRepository generationReviewRepository) : IAIGenerationPrerequisiteValidator, ITransientDependency
{
    public Task EnsureAvailableAsync(string operationType, AIGenerationSubmissionDto request) => operationType switch
    {
        AIGenerationOperations.AttachmentSummary => EnsureAttachmentSummaryAvailableAsync(request.ApplicationId),
        AIGenerationOperations.ApplicationAnalysis => EnsureApplicationAnalysisAvailableAsync(request.ApplicationId),
        AIGenerationOperations.ApplicationScoring => EnsureApplicationScoringAvailableAsync(request.ApplicationId),
        AIGenerationOperations.FormMapping => EnsureFormMappingAvailableAsync(request.ApplicationFormVersionId.GetValueOrDefault()),
        AIGenerationOperations.FormWorksheet => EnsureFormWorksheetAvailableAsync(request.ApplicationFormVersionId.GetValueOrDefault()),
        AIGenerationOperations.FormScoresheet => EnsureFormScoresheetAvailableAsync(request.ApplicationFormVersionId.GetValueOrDefault()),
        _ => throw new UserFriendlyException($"Unsupported AI generation operation type: {operationType}")
    };

    public async Task EnsureAttachmentSummaryAvailableAsync(Guid applicationId)
    {
        var attachmentQuery = await applicationChefsFileAttachmentRepository.GetQueryableAsync();
        var hasAttachments = await asyncExecuter.AnyAsync(attachmentQuery.Where(a => a.ApplicationId == applicationId));
        if (!hasAttachments) throw new BusinessException(AIErrorCodes.NoAttachmentsAvailable);
    }

    public async Task EnsureApplicationAnalysisAvailableAsync(Guid applicationId)
    {
        var submission = await applicationFormSubmissionRepository.GetByApplicationAsync(applicationId);
        if (submission == null || string.IsNullOrWhiteSpace(submission.Submission))
            throw new BusinessException(AIErrorCodes.ApplicationAnalysisRequiresSubmission);
    }

    public async Task EnsureApplicationScoringAvailableAsync(Guid applicationId)
    {
        var application = await applicationRepository.GetAsync(applicationId);
        var applicationForm = await applicationFormRepository.GetAsync(application.ApplicationFormId);
        if (applicationForm.ScoresheetId == null)
            throw new BusinessException(AIErrorCodes.ScoringRequiresScoresheet);
        var scoresheet = await scoresheetRepository.GetWithChildrenAsync(applicationForm.ScoresheetId.Value);
        if (scoresheet == null || !scoresheet.Sections.Any() || !scoresheet.Sections.SelectMany(s => s.Fields).Any())
            throw new BusinessException(AIErrorCodes.ScoringRequiresScoresheetFields);
    }

    public async Task EnsureFormMappingAvailableAsync(Guid applicationFormVersionId)
    {
        var formVersion = await applicationFormVersionRepository.FindAsync(applicationFormVersionId);
        if (formVersion == null) throw new BusinessException(AIErrorCodes.FormMappingRequiresFormVersion);
        await EnsureNoActiveReviewAsync(AIGenerationOperations.FormMapping, applicationFormVersionId);
    }

    public async Task EnsureFormWorksheetAvailableAsync(Guid applicationFormVersionId)
    {
        var formVersion = await applicationFormVersionRepository.FindAsync(applicationFormVersionId);
        if (formVersion == null) throw new BusinessException(AIErrorCodes.FormWorksheetRequiresFormVersion);
        await EnsureNoActiveReviewAsync(AIGenerationOperations.FormWorksheet, applicationFormVersionId);
    }

    public async Task EnsureFormScoresheetAvailableAsync(Guid applicationFormVersionId)
    {
        var formVersion = await applicationFormVersionRepository.FindAsync(applicationFormVersionId);
        if (formVersion == null) throw new BusinessException(AIErrorCodes.FormScoresheetRequiresFormVersion);
        await EnsureNoActiveReviewAsync(AIGenerationOperations.FormScoresheet, applicationFormVersionId);
    }

    private async Task EnsureNoActiveReviewAsync(string operation, Guid formVersionId)
    {
        var review = await generationReviewRepository.FindLatestByOperationAndFormVersionAsync(operation, formVersionId);
        if (review?.Status == GenerationReviewStatus.Active)
            throw new BusinessException(AIErrorCodes.FormGenerationReviewActive);
    }
}
