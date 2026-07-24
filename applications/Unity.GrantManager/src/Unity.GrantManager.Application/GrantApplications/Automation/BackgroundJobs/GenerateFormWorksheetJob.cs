using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.AI.Cooldown;
using Unity.AI.Operations;
using Unity.AI.Requests;
using Unity.AI.Responses;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.ApplicationForms.Mapping;
using Unity.GrantManager.Applications;
using Unity.Flex.Domain.WorksheetLinks;
using Unity.Flex.Domain.Worksheets;
using Unity.Flex.Worksheets;
using Unity.Flex.Worksheets.Definitions;
using Unity.Modules.Shared.Correlation;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;

namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

public class GenerateFormWorksheetJob(
    IApplicationFormVersionRepository applicationFormVersionRepository,
    IApplicationFormRepository applicationFormRepository,
    IWorksheetRepository worksheetRepository,
    IWorksheetLinkRepository worksheetLinkRepository,
    IApplicationFormVersionMappingReadService mappingReadService,
    IFormWorksheetService aiService,
    IRepository<AIGenerationRequest, Guid> generationRequestRepository,
    ICurrentTenant currentTenant,
    IUnitOfWorkManager unitOfWorkManager,
    IAICooldownService aiCooldownService,
    ILogger<GenerateFormWorksheetJob> logger) : AsyncBackgroundJob<GenerateFormWorksheetBackgroundJobArgs>, ITransientDependency
{
    private static readonly JsonSerializerOptions CaseInsensitiveJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public override async Task ExecuteAsync(GenerateFormWorksheetBackgroundJobArgs args)
    {
        using var logScope = AIGenerationLogScope.Begin(
            logger,
            AIGenerationRequestKeyHelper.FormWorksheetOperationType,
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
                var worksheetName = BuildWorksheetName(formVersion.Id, applicationForm.Id);
                var existingWorksheet = await worksheetRepository.GetByNameAsync(worksheetName, true);
                var mappingReadModel = await mappingReadService.GetAsync(formVersion.Id);
                if (existingWorksheet == null || existingWorksheet.Published)
                {
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
                    if (existingWorksheet == null)
                    {
                        var worksheet = BuildWorksheet(suggestions, worksheetName);
                        worksheet.SetPublished(false);
                        await worksheetRepository.InsertAsync(worksheet);
                    }
                    else
                    {
                        var existingLink = await worksheetLinkRepository.GetExistingLinkAsync(
                            existingWorksheet.Id,
                            formVersion.Id,
                            CorrelationConsts.FormVersion);
                        if (existingLink != null)
                        {
                            await worksheetLinkRepository.DeleteAsync(existingLink, true);
                        }

                        RebuildWorksheet(existingWorksheet, suggestions);
                        existingWorksheet.SetPublished(false);
                        await worksheetRepository.UpdateAsync(existingWorksheet);
                    }
                }
                else
                {
                    logger.LogInformation(
                        "An unpublished AI worksheet already exists for form version {FormVersionId}; leaving it available for review.",
                        formVersion.Id);
                }

                await AIGenerationRequestJobHelper.StampCooldownBestEffortAsync(aiCooldownService, logger, args.RequestedByUserId, args.ApplicationId, AIGenerationRequestKeyHelper.FormWorksheetOperationType);
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
            field.Key = field.Key?.Trim() ?? string.Empty;
            field.Label = field.Label?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(field.Key)
                || string.IsNullOrWhiteSpace(field.Label)
                || !keys.Add(field.Key)
                || !Enum.TryParse<CustomFieldType>(field.Type, false, out var type)
                || !SupportedSuggestionTypes.Contains(type))
            {
                throw new InvalidOperationException("Worksheet generation returned an unusable worksheet definition.");
            }

            field.ResolvedType = type;
        }

        return dto.Fields;
    }

    private static string BuildWorksheetName(Guid formVersionId, Guid formId)
    {
        return $"ai-form-{formId}-version-{formVersionId}-worksheet";
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
        public string? Key { get; set; }
        public string? Label { get; set; }
        public string? Type { get; set; }
        public CustomFieldType ResolvedType { get; set; }
    }

}
