using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.AI;
using Unity.AI.Domain;
using Unity.AI.Requests;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Applications;
using Unity.AI.RateLimit;
using Unity.Flex.Domain.Scoresheets;
using Unity.Flex.Scoresheets;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;

namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

public class GenerateFormScoresheetJob(
    IApplicationFormVersionRepository applicationFormVersionRepository,
    IApplicationFormRepository applicationFormRepository,
    IScoresheetRepository scoresheetRepository,
    IAIService aiService,
    IRepository<AIGenerationRequest, Guid> generationRequestRepository,
    IRepository<AIOperation, Guid> operationRepository,
    ICurrentTenant currentTenant,
    IUnitOfWorkManager unitOfWorkManager,
    IAIRateLimiter aiRateLimiter,
    ILogger<GenerateFormScoresheetJob> logger) : AsyncBackgroundJob<GenerateFormScoresheetBackgroundJobArgs>, ITransientDependency
{
    public override async Task ExecuteAsync(GenerateFormScoresheetBackgroundJobArgs args)
    {
        using var logScope = AIGenerationLogScope.Begin(
            logger,
            AIGenerationRequestKeyHelper.FormScoresheetOperationType,
            args.ApplicationId,
            args.TenantId,
            args.PromptVersion,
            args.RequestedByUserId);

        using (currentTenant.Change(args.TenantId))
        {
            await AIGenerationRequestJobHelper.MarkRunningInNewUowAsync(
                unitOfWorkManager,
                generationRequestRepository,
                operationRepository,
                args.TenantId,
                args.ApplicationId,
                AIGenerationRequestKeyHelper.FormScoresheetOperationType);

            try
            {
                var formVersion = await applicationFormVersionRepository.GetAsync(args.ApplicationFormVersionId);
                var applicationForm = await applicationFormRepository.GetAsync(formVersion.ApplicationFormId);
                var scoresheetName = BuildScoresheetName(formVersion.Id, applicationForm.Id);
                var existingScoresheet = await scoresheetRepository.GetByNameAsync(scoresheetName, true)
                    ?? (applicationForm.ScoresheetId.HasValue
                        ? await scoresheetRepository.GetWithChildrenAsync(applicationForm.ScoresheetId.Value)
                        : null);

                var promptData = new
                {
                    applicationFormVersionId = formVersion.Id,
                    chefsFormVersionGuid = formVersion.ChefsFormVersionGuid,
                    applicationFormId = applicationForm.Id,
                    formName = applicationForm.ApplicationFormName,
                    scoresheetId = applicationForm.ScoresheetId,
                    existingScoresheet = existingScoresheet == null
                        ? null
                        : new
                        {
                            existingScoresheet.Id,
                            existingScoresheet.Title,
                            existingScoresheet.Name,
                            existingScoresheet.Version,
                            existingScoresheet.Order,
                            existingScoresheet.Published,
                            existingScoresheet.ReportColumns,
                            existingScoresheet.ReportKeys,
                            existingScoresheet.ReportViewName,
                            sections = existingScoresheet.Sections.Select(section => new
                            {
                                section.Name,
                                section.Order,
                                fields = section.Fields.Select(field => new
                                {
                                    field.Name,
                                    field.Label,
                                    field.Description,
                                    field.Order,
                                    field.Type,
                                    field.Enabled,
                                    field.Definition
                                })
                            })
                        }
                };

                var scoresheetJson = await aiService.GenerateFormScoresheetAsync(new MappingSuggestionRequest
                {
                    Data = JsonSerializer.SerializeToElement(promptData),
                    PromptVersion = args.PromptVersion
                });

                var importDto = ParseScoresheetDefinition(scoresheetJson);
                var scoresheet = existingScoresheet ?? await scoresheetRepository.InsertAsync(BuildScoresheet(importDto, scoresheetJson, scoresheetName));
                RebuildScoresheet(scoresheet, importDto, scoresheetJson, scoresheetName);
                scoresheet.Published = true;
                await scoresheetRepository.UpdateAsync(scoresheet);

                applicationForm.ScoresheetId = scoresheet.Id;
                await applicationFormRepository.UpdateAsync(applicationForm);

                await AIGenerationRequestJobHelper.StampRateLimitBestEffortAsync(aiRateLimiter, logger, args.RequestedByUserId, args.ApplicationId, AIGenerationRequestKeyHelper.FormScoresheetOperationType);
                await AIGenerationRequestJobHelper.MarkCompletedInNewUowAsync(
                    unitOfWorkManager,
                    generationRequestRepository,
                    operationRepository,
                    args.TenantId,
                    args.ApplicationId,
                    AIGenerationRequestKeyHelper.FormScoresheetOperationType);
            }
            catch (Exception ex)
            {
                await AIGenerationRequestJobHelper.MarkFailedInNewUowAsync(
                    unitOfWorkManager,
                    generationRequestRepository,
                    operationRepository,
                    args.TenantId,
                    args.ApplicationId,
                    AIGenerationRequestKeyHelper.FormScoresheetOperationType,
                    ex.Message);
                throw;
            }
        }
    }

    private static CreateScoresheetDto ParseScoresheetDefinition(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("Scoresheet generation returned empty content.");
        }

        var dto = JsonSerializer.Deserialize<CreateScoresheetDto>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return dto ?? throw new InvalidOperationException("Scoresheet generation returned an unusable scoresheet definition.");
    }

    private static string BuildScoresheetName(Guid formVersionId, Guid formId)
    {
        return $"ai-form-{formId}-version-{formVersionId}-scoresheet";
    }

    private static Scoresheet BuildScoresheet(CreateScoresheetDto dto, string json, string scoresheetName)
    {
        var scoresheet = new Scoresheet(Guid.NewGuid(), dto.Title, scoresheetName);
        var parsed = JsonSerializer.Deserialize<JsonElement>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (!parsed.TryGetProperty("Version", out var versionElement) || versionElement.ValueKind != JsonValueKind.Number)
        {
            throw new InvalidOperationException("Scoresheet generation returned a definition without a valid Version.");
        }

        scoresheet.Version = versionElement.GetUInt32();

        foreach (var section in parsed.GetProperty("Sections").EnumerateArray())
        {
            var sectionName = section.GetProperty("Name").GetString() ?? string.Empty;
            var sectionOrder = section.GetProperty("Order").GetUInt32();
            var scoresheetSection = new ScoresheetSection(Guid.NewGuid(), sectionName, sectionOrder);
            scoresheet.AddSection(scoresheetSection);

            foreach (var field in section.GetProperty("Fields").EnumerateArray())
            {
                var question = new Question(
                    Guid.NewGuid(),
                    field.GetProperty("Name").GetString() ?? string.Empty,
                    field.GetProperty("Label").GetString() ?? string.Empty,
                    (Unity.Flex.Scoresheets.Enums.QuestionType)field.GetProperty("Type").GetInt32(),
                    field.GetProperty("Order").GetUInt32(),
                    field.TryGetProperty("Description", out var description) && description.ValueKind != JsonValueKind.Null
                        ? description.GetString()
                        : null,
                    field.TryGetProperty("Definition", out var definition) ? definition.GetString() : null);
                question.SectionId = scoresheetSection.Id;
                scoresheetSection.Fields.Add(question);
            }
        }

        scoresheet.SetReportingFields(
            parsed.GetProperty("ReportKeys").GetString() ?? string.Empty,
            parsed.GetProperty("ReportColumns").GetString() ?? string.Empty,
            parsed.GetProperty("ReportViewName").GetString() ?? string.Empty);

        return scoresheet;
    }

    private static void RebuildScoresheet(Scoresheet scoresheet, CreateScoresheetDto dto, string json, string scoresheetName)
    {
        var parsed = JsonSerializer.Deserialize<JsonElement>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        scoresheet.SetName(scoresheetName);
        scoresheet.Title = dto.Title;
        scoresheet.Version = parsed.GetProperty("Version").GetUInt32();
        scoresheet.SetReportingFields(
            parsed.GetProperty("ReportKeys").GetString() ?? string.Empty,
            parsed.GetProperty("ReportColumns").GetString() ?? string.Empty,
            parsed.GetProperty("ReportViewName").GetString() ?? string.Empty);

        scoresheet.Sections.Clear();

        foreach (var section in parsed.GetProperty("Sections").EnumerateArray())
        {
            var sectionName = section.GetProperty("Name").GetString() ?? string.Empty;
            var sectionOrder = section.GetProperty("Order").GetUInt32();
            var scoresheetSection = new ScoresheetSection(Guid.NewGuid(), sectionName, sectionOrder);
            scoresheet.AddSection(scoresheetSection);

            foreach (var field in section.GetProperty("Fields").EnumerateArray())
            {
                var question = new Question(
                    Guid.NewGuid(),
                    field.GetProperty("Name").GetString() ?? string.Empty,
                    field.GetProperty("Label").GetString() ?? string.Empty,
                    (Unity.Flex.Scoresheets.Enums.QuestionType)field.GetProperty("Type").GetInt32(),
                    field.GetProperty("Order").GetUInt32(),
                    field.TryGetProperty("Description", out var description) && description.ValueKind != JsonValueKind.Null
                        ? description.GetString()
                        : null,
                    field.TryGetProperty("Definition", out var definition) ? definition.GetString() : null);
                question.SectionId = scoresheetSection.Id;
                scoresheetSection.Fields.Add(question);
            }
        }
    }
}
