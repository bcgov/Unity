using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.AI.Generation;
using Unity.AI.Operations;
using Unity.AI.Localization;
using Unity.AI.Requests;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.ApplicationForms.Mapping;
using Unity.GrantManager.Applications;
using Unity.Flex.Domain.Scoresheets;
using Unity.Flex.Domain.ScoresheetInstances;
using Unity.Flex.Scoresheets;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;
using Volo.Abp.Guids;

using Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

namespace Unity.GrantManager.GrantApplications.Automation.Operations.FormScoresheet;

public sealed class FormScoresheetOperationExecutor(
    IApplicationFormVersionRepository applicationFormVersionRepository,
    IApplicationFormRepository applicationFormRepository,
    IScoresheetRepository scoresheetRepository,
    IScoresheetInstanceRepository scoresheetInstanceRepository,
    IFormScoresheetService aiService,
    IGenerationReviewRepository generationReviewRepository,
    IGuidGenerator guidGenerator,
    IStringLocalizer<AIResource> localizer) : AIGenerationOperationExecutor, ITransientDependency
{
    private static readonly JsonSerializerOptions CaseInsensitiveJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public override string OperationType => AIGenerationOperations.FormScoresheet;

    protected override async Task<bool> ExecuteAsync(AIGenerationBackgroundJobArgs args)
    {
        var applicationFormVersionId = args.ApplicationFormVersionId
            ?? throw new InvalidOperationException(localizer[AILocalizationKeys.ScoresheetGenerationRequiresFormVersion]);
        var formVersion = await applicationFormVersionRepository.GetAsync(applicationFormVersionId);
        var applicationForm = await applicationFormRepository.GetAsync(formVersion.ApplicationFormId);
        var scoresheetName = AiScoresheetSuggestionName.Build(applicationForm.Id, formVersion.Id);
        var existingScoresheet = await scoresheetRepository.GetByNameAsync(scoresheetName, true);
        var review = await generationReviewRepository.FindLatestByOperationAndFormVersionAsync(
            AIGenerationOperations.FormScoresheet,
            applicationFormVersionId);

        if (review?.Status == GenerationReviewStatus.Active)
        {
            return false;
        }

        if (existingScoresheet is { Published: true } || existingScoresheet?.IsArchived == true
            || existingScoresheet != null && await scoresheetInstanceRepository.AnyByScoresheetAsync(existingScoresheet.Id))
        {
            throw new InvalidOperationException(localizer[AILocalizationKeys.ScoresheetGenerationProtected]);
        }

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

        var scoresheetResponse = await aiService.GenerateFormScoresheetAsync(new FormScoresheetRequest
        {
            Data = JsonSerializer.SerializeToElement(promptData),
            PromptVersion = args.PromptVersion
        });

        var scoresheetJson = scoresheetResponse.Scoresheet;
        if (!string.IsNullOrWhiteSpace(scoresheetResponse.FailureReason))
        {
            throw new InvalidOperationException(
                localizer[AILocalizationKeys.ScoresheetGenerationInvalidOutput, scoresheetResponse.FailureReason]);
        }

        var importDto = ParseScoresheetDefinition(scoresheetJson);
        var parsed = ParseScoresheetElement(scoresheetJson);
        if (!HasGeneratedQuestions(parsed))
        {
            if (review == null || review.Status != GenerationReviewStatus.Active)
            {
                review = new GenerationReview(
                    guidGenerator.Create(),
                    AIGenerationOperations.FormScoresheet,
                    applicationFormVersionId,
                    review?.Sequence + 1 ?? 1);
                await generationReviewRepository.InsertAsync(review);
            }

            review.Complete();
            await generationReviewRepository.UpdateAsync(review, true);
            return false;
        }

        var scoresheet = existingScoresheet == null
            ? BuildScoresheet(importDto, scoresheetJson, scoresheetName)
            : RebuildScoresheet(existingScoresheet, importDto, scoresheetJson, scoresheetName);
        scoresheet.Published = false;
        if (existingScoresheet == null)
        {
            await scoresheetRepository.InsertAsync(scoresheet);
        }
        else
        {
            await scoresheetRepository.UpdateAsync(scoresheet);
        }

        if (review == null || review.Status != GenerationReviewStatus.Active)
        {
            review = new GenerationReview(
                guidGenerator.Create(),
                AIGenerationOperations.FormScoresheet,
                applicationFormVersionId,
                review?.Sequence + 1 ?? 1);
            await generationReviewRepository.InsertAsync(review);
        }

        await generationReviewRepository.UpdateAsync(review, true);

        return true;
    }

    private CreateScoresheetDto ParseScoresheetDefinition(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException(localizer[AILocalizationKeys.ScoresheetGenerationEmpty]);
        }

        var dto = JsonSerializer.Deserialize<CreateScoresheetDto>(json, CaseInsensitiveJsonOptions);

        if (dto == null || string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new InvalidOperationException(localizer[AILocalizationKeys.ScoresheetGenerationUnusable]);
        }

        return dto;
    }

    private Scoresheet BuildScoresheet(CreateScoresheetDto dto, string json, string scoresheetName)
    {
        var scoresheet = new Scoresheet(Guid.NewGuid(), dto.Title, scoresheetName);
        var parsed = ParseScoresheetElement(json);
        if (!TryGetNumberProperty(parsed, "Version", out var version))
        {
            throw new InvalidOperationException(localizer[AILocalizationKeys.ScoresheetGenerationNoVersion]);
        }

        scoresheet.Version = version;

        if (!TryGetProperty(parsed, "Sections", out var sectionsElement) || sectionsElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException(localizer[AILocalizationKeys.ScoresheetGenerationNoSections]);
        }

        foreach (var section in sectionsElement.EnumerateArray())
        {
            var sectionName = GetRequiredStringProperty(section, "Name", "section");
            var sectionOrder = GetRequiredNumberProperty(section, "Order", "section");
            var scoresheetSection = new ScoresheetSection(Guid.NewGuid(), sectionName, sectionOrder);
            scoresheet.AddSection(scoresheetSection);

            if (!TryGetProperty(section, "Fields", out var fieldsElement) || fieldsElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException(localizer[AILocalizationKeys.ScoresheetGenerationSectionNoFields, sectionName]);
            }

            foreach (var field in fieldsElement.EnumerateArray())
            {
                var question = new Question(
                    Guid.NewGuid(),
                    GetRequiredStringProperty(field, "Name", "field"),
                    GetRequiredStringProperty(field, "Label", "field"),
                    (Unity.Flex.Scoresheets.Enums.QuestionType)GetRequiredNumberProperty(field, "Type", "field"),
                    GetRequiredNumberProperty(field, "Order", "field"),
                    TryGetProperty(field, "Description", out var description) && description.ValueKind != JsonValueKind.Null
                        ? description.GetString()
                        : null,
                    TryGetProperty(field, "Definition", out var definition) && definition.ValueKind != JsonValueKind.Null
                        ? definition.GetString()
                        : null);
                question.SectionId = scoresheetSection.Id;
                scoresheetSection.Fields.Add(question);
            }
        }

        scoresheet.SetReportingFields(
            GetRequiredStringProperty(parsed, "ReportKeys", "scoresheet", allowEmpty: true),
            GetRequiredStringProperty(parsed, "ReportColumns", "scoresheet", allowEmpty: true),
            GetRequiredStringProperty(parsed, "ReportViewName", "scoresheet", allowEmpty: true));

        return scoresheet;
    }

    private Scoresheet RebuildScoresheet(Scoresheet scoresheet, CreateScoresheetDto dto, string json, string scoresheetName)
    {
        var parsed = ParseScoresheetElement(json);
        if (!TryGetNumberProperty(parsed, "Version", out var version))
        {
            throw new InvalidOperationException(localizer[AILocalizationKeys.ScoresheetGenerationNoVersion]);
        }

        scoresheet.SetName(scoresheetName);
        scoresheet.Title = dto.Title;
        scoresheet.Version = version;
        scoresheet.SetReportingFields(
            GetRequiredStringProperty(parsed, "ReportKeys", "scoresheet", allowEmpty: true),
            GetRequiredStringProperty(parsed, "ReportColumns", "scoresheet", allowEmpty: true),
            GetRequiredStringProperty(parsed, "ReportViewName", "scoresheet", allowEmpty: true));

        scoresheet.Sections.Clear();

        if (!TryGetProperty(parsed, "Sections", out var sectionsElement) || sectionsElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException(localizer[AILocalizationKeys.ScoresheetGenerationNoSections]);
        }

        foreach (var section in sectionsElement.EnumerateArray())
        {
            var sectionName = GetRequiredStringProperty(section, "Name", "section");
            var sectionOrder = GetRequiredNumberProperty(section, "Order", "section");
            var scoresheetSection = new ScoresheetSection(Guid.NewGuid(), sectionName, sectionOrder);
            scoresheet.AddSection(scoresheetSection);

            if (!TryGetProperty(section, "Fields", out var fieldsElement) || fieldsElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException(localizer[AILocalizationKeys.ScoresheetGenerationSectionNoFields, sectionName]);
            }

            foreach (var field in fieldsElement.EnumerateArray())
            {
                var question = new Question(
                    Guid.NewGuid(),
                    GetRequiredStringProperty(field, "Name", "field"),
                    GetRequiredStringProperty(field, "Label", "field"),
                    (Unity.Flex.Scoresheets.Enums.QuestionType)GetRequiredNumberProperty(field, "Type", "field"),
                    GetRequiredNumberProperty(field, "Order", "field"),
                    TryGetProperty(field, "Description", out var description) && description.ValueKind != JsonValueKind.Null
                        ? description.GetString()
                        : null,
                    TryGetProperty(field, "Definition", out var definition) && definition.ValueKind != JsonValueKind.Null
                        ? definition.GetString()
                        : null);
                question.SectionId = scoresheetSection.Id;
                scoresheetSection.Fields.Add(question);
            }
        }

        return scoresheet;
    }

    private static bool HasGeneratedQuestions(JsonElement parsed)
    {
        return TryGetProperty(parsed, "Sections", out var sections)
            && sections.ValueKind == JsonValueKind.Array
            && sections.EnumerateArray().Any(section =>
                TryGetProperty(section, "Fields", out var fields)
                && fields.ValueKind == JsonValueKind.Array
                && fields.GetArrayLength() > 0);
    }

    private static JsonElement ParseScoresheetElement(string json)
    {
        return JsonSerializer.Deserialize<JsonElement>(json, CaseInsensitiveJsonOptions);
    }

    private static bool TryGetNumberProperty(JsonElement element, string propertyName, out uint value)
    {
        if (TryGetProperty(element, propertyName, out var property) && property.ValueKind == JsonValueKind.Number)
        {
            value = property.GetUInt32();
            return true;
        }

        value = default;
        return false;
    }

    private string GetRequiredStringProperty(JsonElement element, string propertyName, string sourceName, bool allowEmpty = false)
    {
        if (TryGetProperty(element, propertyName, out var property)
            && property.ValueKind == JsonValueKind.String
            && (allowEmpty || !string.IsNullOrWhiteSpace(property.GetString())))
        {
            return property.GetString()!;
        }

        throw new InvalidOperationException(localizer[AILocalizationKeys.ScoresheetGenerationPropertyInvalid, sourceName, propertyName]);
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement property)
    {
        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out property))
        {
            return true;
        }

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var candidate in element.EnumerateObject())
            {
                if (string.Equals(candidate.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    property = candidate.Value;
                    return true;
                }
            }
        }

        property = default;
        return false;
    }

    private uint GetRequiredNumberProperty(JsonElement element, string propertyName, string sourceName)
    {
        if (TryGetNumberProperty(element, propertyName, out var value))
        {
            return value;
        }

        throw new InvalidOperationException(localizer[AILocalizationKeys.ScoresheetGenerationPropertyInvalid, sourceName, propertyName]);
    }
}
