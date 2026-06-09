using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.AI;
using Unity.AI.Localization;
using Unity.AI.Operations;
using Unity.AI.Prompts;
using Unity.AI.Permissions;
using Unity.AI.Settings;
using Unity.Flex.Domain.Scoresheets;
using Unity.GrantManager.Applications;
using Volo.Abp;
using Volo.Abp.EventBus.Local;

namespace Unity.GrantManager.GrantApplications;

[Authorize(AIPermissions.Analysis.GenerateScoring)]
public class ApplicationScoringAppService(
    IApplicationScoringService applicationScoringService,
    IApplicationRepository applicationRepository,
    IApplicationFormRepository applicationFormRepository,
    IApplicationFormSubmissionRepository applicationFormSubmissionRepository,
    IApplicationFormVersionRepository applicationFormVersionRepository,
    IApplicationChefsFileAttachmentRepository applicationChefsFileAttachmentRepository,
    IScoresheetRepository scoresheetRepository,
    AIFeatureGuard featureGuard,
    ILocalEventBus localEventBus,
    ILogger<ApplicationScoringAppService> logger,
    IStringLocalizer<AIResource> localizer)
    : AIAppService, IApplicationScoringAppService
{
    public virtual async Task<ApplicationScoringResultDto> GenerateApplicationScoringAsync(Guid applicationId, string? promptVersion = null)
    {
        await featureGuard.EnsureEnabledAsync(
            AIFeatures.Scoring,
            AILocalizationKeys.ScoringDisabled);

        var input = await BuildInputAsync(applicationId, promptVersion);
        await applicationScoringService.RegenerateAndSaveAsync(input);

        if (UnitOfWorkManager.Current != null)
        {
            var capturedId = applicationId;
            UnitOfWorkManager.Current.OnCompleted(async () =>
            {
                await localEventBus.PublishAsync(new ApplicationAIScoringGeneratedEvent
                {
                    ApplicationId = capturedId
                });
            });
        }
        else
        {
            await localEventBus.PublishAsync(new ApplicationAIScoringGeneratedEvent
            {
                ApplicationId = applicationId
            });
        }

        return new ApplicationScoringResultDto
        {
            Completed = true
        };
    }

    // Internal-only: no HTTP endpoint, no auth check — safe for background job callers
    [AllowAnonymous]
    [RemoteService(IsEnabled = false)]
    public virtual async Task<ApplicationScoringResultDto> GenerateApplicationScoringForPipelineAsync(Guid applicationId, string? promptVersion = null)
    {
        var input = await BuildInputAsync(applicationId, promptVersion);
        await applicationScoringService.RegenerateAndSaveAsync(input);
        return new ApplicationScoringResultDto { Completed = true };
    }

    private async Task<ApplicationScoringOperationInputDto> BuildInputAsync(Guid applicationId, string? promptVersion)
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

        var attachments = await applicationChefsFileAttachmentRepository.GetListAsync(a => a.ApplicationId == applicationId);
        var attachmentSummaries = PromptDataPayloadBuilder.BuildAttachmentSummaries(
            attachments,
            excludeWhitespaceOnlySummaries: false);

        var formSubmission = await applicationFormSubmissionRepository.GetByApplicationAsync(applicationId);
        var formSchema = await GetFormSchemaAsync(formSubmission?.ApplicationFormVersionId);
        var promptData = PromptDataPayloadBuilder.BuildPromptDataPayload(application, formSubmission, formSchema, logger);

        var sections = scoresheet.Sections
            .OrderBy(s => s.Order)
            .Select(section => new ApplicationScoringSectionOperationInputDto
            {
                SectionName = section.Name,
                SectionSchema = JsonSerializer.SerializeToElement(BuildSectionQuestionsData(section), AIJsonDefaults.IndentedCamelCase)
            })
            .ToList();

        return new ApplicationScoringOperationInputDto
        {
            ApplicationId = applicationId,
            Data = promptData,
            Attachments = attachmentSummaries,
            Sections = sections,
            PromptVersion = promptVersion
        };
    }

    private async Task<string?> GetFormSchemaAsync(Guid? formVersionId)
    {
        if (formVersionId == null)
        {
            return null;
        }

        try
        {
            var formVersion = await applicationFormVersionRepository.GetAsync(formVersionId.Value);
            return string.IsNullOrWhiteSpace(formVersion?.FormSchema) ? null : formVersion.FormSchema;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to load form schema for application scoring prompt data generation for form version {FormVersionId}.", formVersionId);
            return null;
        }
    }

    private static List<object> BuildSectionQuestionsData(ScoresheetSection section)
    {
        var sectionQuestionsData = new List<object>();
        foreach (var field in section.Fields.OrderBy(f => f.Order))
        {
            var options = ExtractSelectListOptions(field);
            sectionQuestionsData.Add(new
            {
                id = field.Id.ToString(),
                section = section.Name,
                question = field.Label,
                description = field.Description,
                type = field.Type.ToString(),
                options,
                allowed_answers = ExtractSelectListOptionNumbers(options)
            });
        }

        return sectionQuestionsData;
    }

    private static object[]? ExtractSelectListOptions(Question field)
    {
        if (field.Type != Unity.Flex.Scoresheets.Enums.QuestionType.SelectList || string.IsNullOrEmpty(field.Definition))
        {
            return null;
        }

        try
        {
            var definition = JsonSerializer.Deserialize<Unity.Flex.Worksheets.Definitions.QuestionSelectListDefinition>(field.Definition);
            if (definition?.Options != null && definition.Options.Count > 0)
            {
                return definition.Options
                    .Select((option, index) =>
                        (object)new
                        {
                            number = index + 1,
                            value = option.Value
                        })
                    .ToArray();
            }
        }
        catch (JsonException)
        {
            // Ignore malformed definition and return null options.
        }

        return null;
    }

    private static string[]? ExtractSelectListOptionNumbers(object[]? options)
    {
        if (options == null || options.Length == 0)
        {
            return null;
        }

        return options
            .Select((_, index) => (index + 1).ToString())
            .ToArray();
    }
}
