using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Flex.Domain.Scoresheets;
using Unity.GrantManager.Applications;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Linq;

namespace Unity.AI.Operations;

public class AIGenerationPrerequisiteValidator(
    IApplicationRepository applicationRepository,
    IApplicationFormRepository applicationFormRepository,
    IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
    IApplicationChefsFileAttachmentRepository applicationChefsFileAttachmentRepository,
    IScoresheetRepository scoresheetRepository,
    IAsyncQueryableExecuter asyncExecuter) : IAIGenerationPrerequisiteValidator, ITransientDependency
{
    public async Task EnsureAttachmentSummaryAvailableAsync(Guid applicationId)
    {
        var attachmentQuery = await applicationChefsFileAttachmentRepository.GetQueryableAsync();
        var hasAttachments = await asyncExecuter.AnyAsync(attachmentQuery.Where(a => a.ApplicationId == applicationId));
        if (!hasAttachments)
        {
            throw new UserFriendlyException("No attachments are available to summarize.");
        }
    }

    public async Task EnsureApplicationAnalysisAvailableAsync(Guid applicationId)
    {
        await applicationRepository.GetAsync(applicationId);

        var submission = await applicationFormSubmissionRepository.GetByApplicationAsync(applicationId);
        if (submission == null || string.IsNullOrWhiteSpace(submission.Submission))
        {
            throw new UserFriendlyException("AI application analysis requires application submission data.");
        }
    }

    public async Task EnsureApplicationScoringAvailableAsync(Guid applicationId)
    {
        var application = await applicationRepository.GetAsync(applicationId);
        var applicationForm = await applicationFormRepository.GetAsync(application.ApplicationFormId);
        if (applicationForm.ScoresheetId == null)
        {
            throw new UserFriendlyException("AI scoring requires a configured scoresheet.");
        }

        var scoresheet = await scoresheetRepository.GetWithChildrenAsync(applicationForm.ScoresheetId.Value);
        if (scoresheet == null || !scoresheet.Sections.Any() || !scoresheet.Sections.SelectMany(s => s.Fields).Any())
        {
            throw new UserFriendlyException("AI scoring requires a scoresheet with scoring fields.");
        }
    }
}
