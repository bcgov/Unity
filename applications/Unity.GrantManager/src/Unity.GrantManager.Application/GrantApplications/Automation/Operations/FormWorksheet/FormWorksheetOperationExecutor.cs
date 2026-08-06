using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.AI.Generation;
using Unity.AI.Operations;
using Unity.AI.Requests;
using Unity.AI.Responses;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.ApplicationForms.Mapping;
using Unity.GrantManager.Applications;
using Unity.Flex.Domain.Worksheets;
using Unity.Flex.Worksheets;
using Unity.Flex.Worksheets.Definitions;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;
using Volo.Abp.Guids;

using Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

namespace Unity.GrantManager.GrantApplications.Automation.Operations.FormWorksheet;

public sealed class FormWorksheetOperationExecutor(
    IApplicationFormVersionRepository applicationFormVersionRepository,
    IApplicationFormRepository applicationFormRepository,
    IWorksheetRepository worksheetRepository,
    IApplicationFormVersionMappingReadService mappingReadService,
    IFormWorksheetService aiService,
    IGenerationReviewRepository generationReviewRepository,
    IGuidGenerator guidGenerator,
    ILogger<FormWorksheetOperationExecutor> logger) : AIGenerationOperationExecutor, ITransientDependency
{
    private static readonly JsonSerializerOptions CaseInsensitiveJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public override string OperationType => AIGenerationOperations.FormWorksheet;

    protected override async Task<bool> ExecuteAsync(AIGenerationBackgroundJobArgs args)
    {
        var applicationFormVersionId = args.ApplicationFormVersionId
            ?? throw new InvalidOperationException("Form worksheet generation requires an application form version.");
        var formVersion = await applicationFormVersionRepository.GetAsync(applicationFormVersionId);
        var applicationForm = await applicationFormRepository.GetAsync(formVersion.ApplicationFormId);
        var baseWorksheetName = AiWorksheetSuggestionName.Build(applicationForm.Id, formVersion.Id);
        var review = await generationReviewRepository.FindLatestByOperationAndFormVersionAsync(
            AIGenerationOperations.FormWorksheet,
            applicationFormVersionId);
        var worksheetName = baseWorksheetName;
        var existingWorksheet = await worksheetRepository.GetByNameAsync(worksheetName, true);
        if (existingWorksheet?.Published == true && review?.Status != GenerationReviewStatus.Active)
        {
            var suffix = review?.Sequence + 1 ?? 2;
            do
            {
                worksheetName = $"{baseWorksheetName}-{suffix++}";
                existingWorksheet = await worksheetRepository.GetByNameAsync(worksheetName, true);
            }
            while (existingWorksheet != null);
        }
        if (existingWorksheet != null)
        {
            if (existingWorksheet.Published)
            {
                logger.LogWarning(
                    "A published worksheet already uses AI suggestion name {WorksheetName}; leaving it unchanged.",
                    worksheetName);
            }
            else
            {
                logger.LogInformation(
                    "An AI suggestion worksheet is pending review for form version {FormVersionId}; leaving it unchanged.",
                    formVersion.Id);
            }
        }
        else
        {
            var mappingReadModel = await mappingReadService.GetAsync(formVersion.Id);
            var promptData = new
            {
                applicationFormVersionId = formVersion.Id,
                chefsFormVersionGuid = formVersion.ChefsFormVersionGuid,
                applicationFormId = applicationForm.Id,
                formName = applicationForm.ApplicationFormName,
                scoresheetId = applicationForm.ScoresheetId,
                chefsFields = mappingReadModel.ChefsFields,
                unityCoreFields = mappingReadModel.UnityCoreFields,
                existingMapping = formVersion.SubmissionHeaderMapping,
                formSchema = formVersion.FormSchema,
                existingCustomFields = mappingReadModel.Worksheets
                    .SelectMany(worksheet => worksheet.Fields.Select(field => new
                    {
                        worksheetId = worksheet.WorksheetId,
                        worksheetName = worksheet.WorksheetName,
                        field.Name,
                        field.Label,
                        field.Type
                    }))
            };

            var worksheetResponse = await aiService.GenerateFormWorksheetAsync(new FormWorksheetRequest
            {
                Data = JsonSerializer.SerializeToElement(promptData),
                PromptVersion = args.PromptVersion
            });

            var suggestions = ParseWorksheetDefinition(worksheetResponse.Worksheet);
            var worksheet = BuildWorksheet(suggestions, worksheetName);
            worksheet.SetPublished(false);
            await worksheetRepository.InsertAsync(worksheet);

        }

        if (review == null || review.Status != GenerationReviewStatus.Active)
        {
            review = new GenerationReview(
                guidGenerator.Create(),
                AIGenerationOperations.FormWorksheet,
                applicationFormVersionId,
                review?.Sequence + 1 ?? 1);
            await generationReviewRepository.InsertAsync(review);
        }

        await generationReviewRepository.UpdateAsync(review, true);

        return existingWorksheet == null;
    }

    internal static List<AiWorksheetFieldSuggestion> ParseWorksheetDefinition(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("Worksheet generation returned empty content.");
        }

        AiWorksheetSuggestions? dto;
        try
        {
            dto = JsonSerializer.Deserialize<AiWorksheetSuggestions>(json, CaseInsensitiveJsonOptions);
        }
        catch (JsonException)
        {
            throw new InvalidOperationException("Worksheet generation returned an unusable worksheet definition.");
        }

        if (dto?.Fields == null)
        {
            throw new InvalidOperationException("Worksheet generation returned an unusable worksheet definition.");
        }

        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var field in dto.Fields)
        {
            field.Key = (field.Key ?? string.Empty).Trim();
            field.Label = (field.Label ?? string.Empty).Trim();
            field.Type = (field.Type ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(field.Key)
                || string.IsNullOrWhiteSpace(field.Label)
                || !keys.Add(field.Key)
                || field.Type.Any(char.IsDigit)
                || !Enum.TryParse<CustomFieldType>(field.Type, true, out var type)
                || !SupportedSuggestionTypes.Contains(type))
            {
                throw new InvalidOperationException("Worksheet generation returned an unusable worksheet definition.");
            }

            field.ResolvedType = type;
        }

        return dto.Fields;
    }

    internal static Worksheet BuildWorksheet(List<AiWorksheetFieldSuggestion> suggestions, string worksheetName)
    {
        var worksheet = new Worksheet(Guid.NewGuid(), worksheetName, "AI Suggested Fields");
        RebuildWorksheet(worksheet, suggestions);
        return worksheet;
    }

    private static void RebuildWorksheet(Worksheet worksheet, List<AiWorksheetFieldSuggestion> suggestions)
    {
        worksheet.Sections.Clear();

        var section = new WorksheetSection(Guid.NewGuid(), "Suggested Fields").SetOrder(1);
        section.Worksheet = worksheet;
        worksheet.AddSection(section);

        foreach (var (field, index) in suggestions.Select((field, index) => (field, index)))
        {
            var customField = new CustomField(
                Guid.NewGuid(),
                field.Key,
                worksheet.Name,
                field.Label,
                field.ResolvedType,
                DefinitionResolver.Resolve(field.ResolvedType, null));
            customField.Section = section;
            section.AddField(customField);
            customField.SetOrder((uint)(index + 1));
        }
    }

    private static readonly HashSet<CustomFieldType> SupportedSuggestionTypes =
    [
        CustomFieldType.Text, CustomFieldType.TextArea, CustomFieldType.Numeric,
        CustomFieldType.Currency, CustomFieldType.Date, CustomFieldType.DateTime,
        CustomFieldType.Email, CustomFieldType.Phone, CustomFieldType.YesNo,
        CustomFieldType.Checkbox
    ];

    private sealed class AiWorksheetSuggestions
    {
        public List<AiWorksheetFieldSuggestion>? Fields { get; set; }
    }

    internal sealed class AiWorksheetFieldSuggestion
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public CustomFieldType ResolvedType { get; set; }
    }

}
