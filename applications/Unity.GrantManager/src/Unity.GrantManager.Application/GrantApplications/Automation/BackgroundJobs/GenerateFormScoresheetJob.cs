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
    private static readonly JsonSerializerOptions CaseInsensitiveJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

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
                var scoresheet = existingScoresheet == null
                    ? BuildScoresheet(importDto, scoresheetJson, scoresheetName)
                    : RebuildScoresheet(existingScoresheet, importDto, scoresheetJson, scoresheetName);
                scoresheet.Published = true;
                if (existingScoresheet == null)
                {
                    await scoresheetRepository.InsertAsync(scoresheet);
                }
                else
                {
                    await scoresheetRepository.UpdateAsync(scoresheet);
                }

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

        var dto = JsonSerializer.Deserialize<CreateScoresheetDto>(json, CaseInsensitiveJsonOptions);

        return dto ?? throw new InvalidOperationException("Scoresheet generation returned an unusable scoresheet definition.");
    }

    private static string BuildScoresheetName(Guid formVersionId, Guid formId)
    {
        return $"ai-form-{formId}-version-{formVersionId}-scoresheet";
    }

    private static Scoresheet BuildScoresheet(CreateScoresheetDto dto, string json, string scoresheetName)
    {
        var scoresheet = new Scoresheet(Guid.NewGuid(), dto.Title, scoresheetName);
        var parsed = ParseScoresheetElement(json);
        if (!TryGetNumberProperty(parsed, "Version", out var version))
        {
            throw new InvalidOperationException("Scoresheet generation returned a definition without a valid Version.");
        }

        scoresheet.Version = version;

        if (!parsed.TryGetProperty("Sections", out var sectionsElement) || sectionsElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Scoresheet generation returned a definition without Sections.");
        }

        foreach (var section in sectionsElement.EnumerateArray())
        {
            var sectionName = GetRequiredStringProperty(section, "Name", "section");
            var sectionOrder = GetRequiredNumberProperty(section, "Order", "section");
            var scoresheetSection = new ScoresheetSection(Guid.NewGuid(), sectionName, sectionOrder);
            scoresheet.AddSection(scoresheetSection);

            if (!section.TryGetProperty("Fields", out var fieldsElement) || fieldsElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException($"Scoresheet generation returned section '{sectionName}' without Fields.");
            }

            foreach (var field in fieldsElement.EnumerateArray())
            {
                var question = new Question(
                    Guid.NewGuid(),
                    GetRequiredStringProperty(field, "Name", "field"),
                    GetRequiredStringProperty(field, "Label", "field"),
                    (Unity.Flex.Scoresheets.Enums.QuestionType)GetRequiredNumberProperty(field, "Type", "field"),
                    GetRequiredNumberProperty(field, "Order", "field"),
                    field.TryGetProperty("Description", out var description) && description.ValueKind != JsonValueKind.Null
                        ? description.GetString()
                        : null,
                    field.TryGetProperty("Definition", out var definition) && definition.ValueKind != JsonValueKind.Null
                        ? definition.GetString()
                        : null);
                question.SectionId = scoresheetSection.Id;
                scoresheetSection.Fields.Add(question);
            }
        }

        scoresheet.SetReportingFields(
            GetRequiredStringProperty(parsed, "ReportKeys", "scoresheet"),
            GetRequiredStringProperty(parsed, "ReportColumns", "scoresheet"),
            GetRequiredStringProperty(parsed, "ReportViewName", "scoresheet"));

        return scoresheet;
    }

    private static Scoresheet RebuildScoresheet(Scoresheet scoresheet, CreateScoresheetDto dto, string json, string scoresheetName)
    {
        var parsed = ParseScoresheetElement(json);
        if (!TryGetNumberProperty(parsed, "Version", out var version))
        {
            throw new InvalidOperationException("Scoresheet generation returned a definition without a valid Version.");
        }

        scoresheet.SetName(scoresheetName);
        scoresheet.Title = dto.Title;
        scoresheet.Version = version;
        scoresheet.SetReportingFields(
            GetRequiredStringProperty(parsed, "ReportKeys", "scoresheet"),
            GetRequiredStringProperty(parsed, "ReportColumns", "scoresheet"),
            GetRequiredStringProperty(parsed, "ReportViewName", "scoresheet"));

        scoresheet.Sections.Clear();

        if (!parsed.TryGetProperty("Sections", out var sectionsElement) || sectionsElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Scoresheet generation returned a definition without Sections.");
        }

        foreach (var section in sectionsElement.EnumerateArray())
        {
            var sectionName = GetRequiredStringProperty(section, "Name", "section");
            var sectionOrder = GetRequiredNumberProperty(section, "Order", "section");
            var scoresheetSection = new ScoresheetSection(Guid.NewGuid(), sectionName, sectionOrder);
            scoresheet.AddSection(scoresheetSection);

            if (!section.TryGetProperty("Fields", out var fieldsElement) || fieldsElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException($"Scoresheet generation returned section '{sectionName}' without Fields.");
            }

            foreach (var field in fieldsElement.EnumerateArray())
            {
                var question = new Question(
                    Guid.NewGuid(),
                    GetRequiredStringProperty(field, "Name", "field"),
                    GetRequiredStringProperty(field, "Label", "field"),
                    (Unity.Flex.Scoresheets.Enums.QuestionType)GetRequiredNumberProperty(field, "Type", "field"),
                    GetRequiredNumberProperty(field, "Order", "field"),
                    field.TryGetProperty("Description", out var description) && description.ValueKind != JsonValueKind.Null
                        ? description.GetString()
                        : null,
                    field.TryGetProperty("Definition", out var definition) && definition.ValueKind != JsonValueKind.Null
                        ? definition.GetString()
                        : null);
                question.SectionId = scoresheetSection.Id;
                scoresheetSection.Fields.Add(question);
            }
        }

        return scoresheet;
    }

    private static JsonElement ParseScoresheetElement(string json)
    {
        return JsonSerializer.Deserialize<JsonElement>(json, CaseInsensitiveJsonOptions);
    }

    private static bool TryGetNumberProperty(JsonElement element, string propertyName, out uint value)
    {
        if (element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.Number)
        {
            value = property.GetUInt32();
            return true;
        }

        value = default;
        return false;
    }

    private static string GetRequiredStringProperty(JsonElement element, string propertyName, string sourceName)
    {
        if (element.TryGetProperty(propertyName, out var property) && property.ValueKind != JsonValueKind.Null)
        {
            return property.GetString() ?? string.Empty;
        }

        throw new InvalidOperationException($"Scoresheet generation returned a {sourceName} without {propertyName}.");
    }

    private static uint GetRequiredNumberProperty(JsonElement element, string propertyName, string sourceName)
    {
        if (TryGetNumberProperty(element, propertyName, out var value))
        {
            return value;
        }

        throw new InvalidOperationException($"Scoresheet generation returned a {sourceName} without a valid {propertyName}.");
    }
}
