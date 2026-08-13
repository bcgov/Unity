using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.AI.Generation;
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
using Volo.Abp.Guids;

using Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

namespace Unity.GrantManager.GrantApplications.Automation.Operations.FormScoresheet;

public sealed class FormScoresheetOperationExecutor(
    IApplicationFormVersionRepository applicationFormVersionRepository,
    IApplicationFormRepository applicationFormRepository,
    IScoresheetRepository scoresheetRepository,
    IFormScoresheetService aiService,
    IGenerationReviewRepository generationReviewRepository,
    IGuidGenerator guidGenerator) : AIGenerationOperationExecutor, ITransientDependency
{
    private static readonly JsonSerializerOptions CaseInsensitiveJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public override string OperationType => AIGenerationOperations.FormScoresheet;

    protected override async Task<bool> ExecuteAsync(AIGenerationBackgroundJobArgs args)
    {
        var applicationFormVersionId = args.ApplicationFormVersionId
            ?? throw new InvalidOperationException("Form scoresheet generation requires an application form version.");
        var formVersion = await applicationFormVersionRepository.GetAsync(applicationFormVersionId);
        var applicationForm = await applicationFormRepository.GetAsync(formVersion.ApplicationFormId);
        var scoresheetName = BuildScoresheetName(formVersion.Id, applicationForm.Id);
        var existingScoresheet = await scoresheetRepository.GetByNameAsync(scoresheetName, true);
        var review = await generationReviewRepository.FindLatestByOperationAndFormVersionAsync(
            AIGenerationOperations.FormScoresheet,
            applicationFormVersionId);

        if (existingScoresheet != null && review?.Status == GenerationReviewStatus.Active)
        {
            return false;
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
                $"Scoresheet generation returned invalid output: {scoresheetResponse.FailureReason}");
        }

        var importDto = ParseScoresheetDefinition(scoresheetJson);
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

    private static CreateScoresheetDto ParseScoresheetDefinition(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("Scoresheet generation returned empty content.");
        }

        var dto = JsonSerializer.Deserialize<CreateScoresheetDto>(json, CaseInsensitiveJsonOptions);

        if (dto == null || string.IsNullOrWhiteSpace(dto.Title) || string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new InvalidOperationException("Scoresheet generation returned an unusable scoresheet definition.");
        }

        return dto;
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

        if (!TryGetProperty(parsed, "Sections", out var sectionsElement) || sectionsElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Scoresheet generation returned a definition without Sections.");
        }

        foreach (var section in sectionsElement.EnumerateArray())
        {
            var sectionName = GetRequiredStringProperty(section, "Name", "section");
            var sectionOrder = GetRequiredNumberProperty(section, "Order", "section");
            var scoresheetSection = new ScoresheetSection(Guid.NewGuid(), sectionName, sectionOrder);
            scoresheet.AddSection(scoresheetSection);

            if (!TryGetProperty(section, "Fields", out var fieldsElement) || fieldsElement.ValueKind != JsonValueKind.Array)
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
            GetRequiredStringProperty(parsed, "ReportKeys", "scoresheet", allowEmpty: true),
            GetRequiredStringProperty(parsed, "ReportColumns", "scoresheet", allowEmpty: true),
            GetRequiredStringProperty(parsed, "ReportViewName", "scoresheet", allowEmpty: true));

        scoresheet.Sections.Clear();

        if (!TryGetProperty(parsed, "Sections", out var sectionsElement) || sectionsElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Scoresheet generation returned a definition without Sections.");
        }

        foreach (var section in sectionsElement.EnumerateArray())
        {
            var sectionName = GetRequiredStringProperty(section, "Name", "section");
            var sectionOrder = GetRequiredNumberProperty(section, "Order", "section");
            var scoresheetSection = new ScoresheetSection(Guid.NewGuid(), sectionName, sectionOrder);
            scoresheet.AddSection(scoresheetSection);

            if (!TryGetProperty(section, "Fields", out var fieldsElement) || fieldsElement.ValueKind != JsonValueKind.Array)
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

    private static string GetRequiredStringProperty(JsonElement element, string propertyName, string sourceName, bool allowEmpty = false)
    {
        if (TryGetProperty(element, propertyName, out var property)
            && property.ValueKind == JsonValueKind.String
            && (allowEmpty || !string.IsNullOrWhiteSpace(property.GetString())))
        {
            return property.GetString()!;
        }

        throw new InvalidOperationException($"Scoresheet generation returned a {sourceName} without a valid {propertyName}.");
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

    private static uint GetRequiredNumberProperty(JsonElement element, string propertyName, string sourceName)
    {
        if (TryGetNumberProperty(element, propertyName, out var value))
        {
            return value;
        }

        throw new InvalidOperationException($"Scoresheet generation returned a {sourceName} without a valid {propertyName}.");
    }
}
