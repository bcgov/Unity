using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.AI.Cooldown;
using Unity.AI.Operations;
using Unity.AI.Requests;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Applications;
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
    IScoresheetSectionRepository scoresheetSectionRepository,
    IQuestionRepository questionRepository,
    IFormScoresheetService aiService,
    IRepository<AIGenerationRequest, Guid> generationRequestRepository,
    ICurrentTenant currentTenant,
    IUnitOfWorkManager unitOfWorkManager,
    IAICooldownService aiCooldownService,
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
                args.TenantId,
                args.ApplicationId,
                args.OperationId);

            try
            {
                var formVersion = await applicationFormVersionRepository.GetAsync(args.ApplicationFormVersionId);
                var applicationForm = await applicationFormRepository.GetAsync(formVersion.ApplicationFormId);
                var scoresheetName = BuildScoresheetName(formVersion.Id, applicationForm.Id);
                var existingScoresheet = await scoresheetRepository.GetByNameAsync(scoresheetName, true);

                var promptData = new
                {
                    applicationFormVersionId = formVersion.Id,
                    chefsFormVersionGuid = formVersion.ChefsFormVersionGuid,
                    applicationFormId = applicationForm.Id,
                    formName = applicationForm.ApplicationFormName,
                    chefsFields = ParseOptionalJsonElement(formVersion.AvailableChefsFields),
                    allowedQuestionTypes = new[]
                    {
                        new { Name = "Number", Value = 1, DefinitionTemplate = "{\"min\":0,\"max\":10,\"required\":true}" },
                        new { Name = "Text", Value = 2, DefinitionTemplate = "{\"required\":true,\"maxLength\":4294967295,\"minLength\":0}" },
                        new { Name = "YesNo", Value = 6, DefinitionTemplate = "{\"yes_value\":0,\"no_value\":0,\"required\":true}" },
                        new { Name = "SelectList", Value = 12, DefinitionTemplate = "{\"options\":[{\"key\":\"key1\",\"value\":\"<option label>\",\"numeric_value\":0}],\"required\":true}" },
                        new { Name = "TextArea", Value = 14, DefinitionTemplate = "{\"rows\":5,\"required\":true,\"maxLength\":4294967295,\"minLength\":0}" }
                    },
                    scoresheetTemplate = new
                    {
                        Title = $"{applicationForm.ApplicationFormName} Scoresheet",
                        Name = scoresheetName,
                        Version = existingScoresheet == null ? 1u : existingScoresheet.Version + 1u,
                        Order = 0,
                        Published = false,
                        ReportColumns = string.Empty,
                        ReportKeys = string.Empty,
                        ReportViewName = string.Empty,
                        Sections = new[]
                        {
                            new
                            {
                                Name = "<review criteria section>",
                                Order = 0,
                                Fields = new[]
                                {
                                    new
                                    {
                                        Name = "<stable question name>",
                                        Label = "<review question shown to assessors>",
                                        Description = "<scoring guidance or null>",
                                        Order = 0,
                                        Type = 12,
                                        Enabled = true,
                                        Definition = "{\"options\":[{\"key\":\"key1\",\"value\":\"<option label>\",\"numeric_value\":0}],\"required\":true}"
                                    }
                                }
                            }
                        }
                    }
                };

                var scoresheetResponse = await aiService.GenerateFormScoresheetAsync(new FormScoresheetRequest
                {
                    Data = JsonSerializer.SerializeToElement(promptData),
                    PromptVersion = args.PromptVersion
                });

                var scoresheetJson = scoresheetResponse.Scoresheet;
                var importDto = ParseScoresheetDefinition(scoresheetJson);
                Scoresheet scoresheet;
                if (existingScoresheet == null)
                {
                    scoresheet = BuildScoresheet(importDto, scoresheetJson, scoresheetName);
                    scoresheet.Published = true;
                    await scoresheetRepository.InsertAsync(scoresheet);
                }
                else
                {
                    scoresheet = await RebuildScoresheetAsync(existingScoresheet, importDto, scoresheetJson, scoresheetName);
                    scoresheet.Published = true;
                    await scoresheetRepository.UpdateAsync(scoresheet);
                    await InsertScoresheetChildrenAsync(scoresheet.Id, scoresheetJson);
                }

                applicationForm.ScoresheetId = scoresheet.Id;
                await applicationFormRepository.UpdateAsync(applicationForm);

                await AIGenerationRequestJobHelper.StampCooldownBestEffortAsync(aiCooldownService, logger, args.RequestedByUserId, args.ApplicationId, AIGenerationRequestKeyHelper.FormScoresheetOperationType);
                await AIGenerationRequestJobHelper.MarkCompletedInNewUowAsync(
                    unitOfWorkManager,
                    generationRequestRepository,
                    args.TenantId,
                    args.ApplicationId,
                    args.OperationId);
            }
            catch (Exception ex)
            {
                await AIGenerationRequestJobHelper.MarkFailedInNewUowAsync(
                    unitOfWorkManager,
                    generationRequestRepository,
                    args.TenantId,
                    args.ApplicationId,
                    args.OperationId,
                    ex.Message);
                throw;
            }
        }
    }

    private static JsonElement? ParseOptionalJsonElement(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<JsonElement>(json, CaseInsensitiveJsonOptions);
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

    private async Task<Scoresheet> RebuildScoresheetAsync(Scoresheet scoresheet, CreateScoresheetDto dto, string json, string scoresheetName)
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

        await DeleteExistingScoresheetChildrenAsync(scoresheet);
        scoresheet.Sections.Clear();

        ValidateScoresheetSections(parsed);

        return scoresheet;
    }

    private static void ValidateScoresheetSections(JsonElement parsed)
    {
        if (!parsed.TryGetProperty("Sections", out var sectionsElement) || sectionsElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Scoresheet generation returned a definition without Sections.");
        }

        foreach (var section in sectionsElement.EnumerateArray())
        {
            var sectionName = GetRequiredStringProperty(section, "Name", "section");
            GetRequiredNumberProperty(section, "Order", "section");

            if (!section.TryGetProperty("Fields", out var fieldsElement) || fieldsElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException($"Scoresheet generation returned section '{sectionName}' without Fields.");
            }

            foreach (var field in fieldsElement.EnumerateArray())
            {
                GetRequiredStringProperty(field, "Name", "field");
                GetRequiredStringProperty(field, "Label", "field");
                GetRequiredNumberProperty(field, "Type", "field");
                GetRequiredNumberProperty(field, "Order", "field");
                GetOptionalStringProperty(field, "Description");
                GetRequiredStringProperty(field, "Definition", "field");
            }
        }
    }

    private async Task InsertScoresheetChildrenAsync(Guid scoresheetId, string json)
    {
        var parsed = ParseScoresheetElement(json);
        ValidateScoresheetSections(parsed);
        var sectionsElement = parsed.GetProperty("Sections");

        foreach (var section in sectionsElement.EnumerateArray())
        {
            var scoresheetSection = await scoresheetSectionRepository.InsertAsync(new ScoresheetSection(
                Guid.NewGuid(),
                GetRequiredStringProperty(section, "Name", "section"),
                GetRequiredNumberProperty(section, "Order", "section"),
                scoresheetId));

            var fieldsElement = section.GetProperty("Fields");
            foreach (var field in fieldsElement.EnumerateArray())
            {
                var question = new Question(
                    Guid.NewGuid(),
                    GetRequiredStringProperty(field, "Name", "field"),
                    GetRequiredStringProperty(field, "Label", "field"),
                    (Unity.Flex.Scoresheets.Enums.QuestionType)GetRequiredNumberProperty(field, "Type", "field"),
                    GetRequiredNumberProperty(field, "Order", "field"),
                    GetOptionalStringProperty(field, "Description"),
                    GetRequiredStringProperty(field, "Definition", "field"));
                question.SectionId = scoresheetSection.Id;
                await questionRepository.InsertAsync(question);
            }
        }
    }

    private async Task DeleteExistingScoresheetChildrenAsync(Scoresheet scoresheet)
    {
        foreach (var section in scoresheet.Sections)
        {
            await questionRepository.DeleteManyAsync(section.Fields.Select(question => question.Id));
        }

        await scoresheetSectionRepository.DeleteManyAsync(scoresheet.Sections.Select(section => section.Id));
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

    private static string? GetOptionalStringProperty(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind != JsonValueKind.Null
            ? property.GetString()
            : null;
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
