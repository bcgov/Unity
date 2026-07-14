using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.AI.Cooldown;
using Unity.AI.Operations;
using Unity.AI.Requests;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Flex;
using Unity.Flex.Domain.WorksheetLinks;
using Unity.Flex.Domain.Worksheets;
using Unity.Flex.Worksheets;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.Intakes.Mapping;
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
    IRepository<WorksheetSection, Guid> worksheetSectionRepository,
    IRepository<CustomField, Guid> customFieldRepository,
    IWorksheetLinkRepository worksheetLinkRepository,
    IFormWorksheetService aiService,
    IRepository<AIGenerationRequest, Guid> generationRequestRepository,
    ICurrentTenant currentTenant,
    IUnitOfWorkManager unitOfWorkManager,
    IAICooldownService aiCooldownService,
    ILogger<GenerateFormWorksheetJob> logger) : AsyncBackgroundJob<GenerateFormWorksheetBackgroundJobArgs>, ITransientDependency
{
    private static readonly string[] ExcludedCoreFieldNames =
    [
        nameof(IntakeMapping.ConfirmationId),
        nameof(IntakeMapping.SubmissionDate),
        nameof(IntakeMapping.SubmissionId)
    ];

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

                var promptData = new
                {
                    applicationFormVersionId = formVersion.Id,
                    chefsFormVersionGuid = formVersion.ChefsFormVersionGuid,
                    applicationFormId = applicationForm.Id,
                    formName = applicationForm.ApplicationFormName,
                    chefsFields = ParseOptionalJsonElement(formVersion.AvailableChefsFields),
                    unityCoreFields = BuildUnityCoreFields(),
                    worksheetTemplate = new
                    {
                        Name = worksheetName,
                        Title = $"{applicationForm.ApplicationFormName} Custom Fields",
                        Version = existingWorksheet == null ? 1u : existingWorksheet.Version + 1u,
                        Published = true,
                        Sections = new[]
                        {
                            new
                            {
                                Name = "Custom Fields",
                                Order = 1,
                                Fields = new[]
                                {
                                    new
                                    {
                                        Name = $"custom_{worksheetName}_CustomField1",
                                        Key = "CustomField1",
                                        Label = "Custom Field 1",
                                        Type = 2,
                                        Order = 1,
                                        Enabled = true,
                                        Definition = "{\"required\": false, \"maxLength\": 4294967295, \"minLength\": 0}"
                                    }
                                }
                            }
                        },
                        ReportColumns = "CustomField1",
                        ReportKeys = "CustomField1",
                        ReportViewName = $"Worksheet-{worksheetName}"
                    }
                };

                var worksheetResponse = await aiService.GenerateFormWorksheetAsync(new FormWorksheetRequest
                {
                    Data = JsonSerializer.SerializeToElement(promptData),
                    PromptVersion = args.PromptVersion
                });

                var worksheetJson = worksheetResponse.Worksheet;
                var createDto = ParseWorksheetDefinition(worksheetJson);
                Worksheet worksheet;
                if (existingWorksheet == null)
                {
                    worksheet = BuildWorksheet(createDto, worksheetName);
                    worksheet.SetPublished(true);
                    await worksheetRepository.InsertAsync(worksheet);
                }
                else
                {
                    worksheet = await RebuildWorksheetAsync(existingWorksheet, createDto);
                    worksheet.SetPublished(true);
                    await worksheetRepository.UpdateAsync(worksheet);
                    await InsertWorksheetChildrenAsync(worksheet, createDto);
                }

                await UpsertWorksheetLinkAsync(worksheet.Id, formVersion.Id);

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

    private static JsonElement? ParseOptionalJsonElement(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<JsonElement>(json, CaseInsensitiveJsonOptions);
    }

    private static object[] BuildUnityCoreFields()
    {
        return typeof(IntakeMapping)
            .GetProperties()
            .Select(property => new
            {
                Property = property,
                Browsable = property.GetCustomAttributes(typeof(BrowsableAttribute), true).Cast<BrowsableAttribute>().SingleOrDefault(),
                DisplayName = property.GetCustomAttributes(typeof(DisplayNameAttribute), true).Cast<DisplayNameAttribute>().SingleOrDefault(),
                FieldType = property.GetCustomAttributes(typeof(MapFieldTypeAttribute), true).Cast<MapFieldTypeAttribute>().SingleOrDefault()
            })
            .Where(item => item.Browsable?.IsDefaultAttribute() == true)
            .Where(item => !ExcludedCoreFieldNames.Contains(item.Property.Name, StringComparer.OrdinalIgnoreCase))
            .Select(item => new
            {
                Name = item.Property.Name,
                Type = item.FieldType?.Type ?? "String",
                Label = item.DisplayName?.DisplayName ?? item.Property.Name
            })
            .OrderBy(field => field.Label)
            .Cast<object>()
            .ToArray();
    }

    private static CreateWorksheetDto ParseWorksheetDefinition(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("Worksheet generation returned empty content.");
        }

        var dto = JsonSerializer.Deserialize<CreateWorksheetDto>(json, CaseInsensitiveJsonOptions);

        return dto ?? throw new InvalidOperationException("Worksheet generation returned an unusable worksheet definition.");
    }

    private static string BuildWorksheetName(Guid formVersionId, Guid formId)
    {
        return $"ai-form-{formId}-version-{formVersionId}-worksheet";
    }

    private static Worksheet BuildWorksheet(CreateWorksheetDto dto, string worksheetName)
    {
        var worksheet = new Worksheet(Guid.NewGuid(), worksheetName, dto.Title)
        {
            ReportColumns = dto.ReportColumns,
            ReportKeys = dto.ReportKeys,
            ReportViewName = dto.ReportViewName
        };

        worksheet.SetVersion(dto.Version);
        worksheet.SetPublished(dto.Published);

        foreach (var section in dto.Sections.OrderBy(s => s.Order))
        {
            var worksheetSection = new WorksheetSection(Guid.NewGuid(), section.Name).SetOrder(section.Order);
            worksheetSection.Worksheet = worksheet;
            worksheet.AddSection(worksheetSection);

            foreach (var field in section.Fields)
            {
                var customField = new CustomField(
                    Guid.NewGuid(),
                    field.Key,
                    worksheet.Name,
                    field.Label,
                    field.Type,
                    GetDefinitionText(field.Definition));
                customField.Section = worksheetSection;
                worksheetSection.Fields.Add(customField);
            }
        }

        return worksheet;
    }

    private async Task<Worksheet> RebuildWorksheetAsync(Worksheet worksheet, CreateWorksheetDto dto)
    {
        worksheet.SetName(worksheet.Name);
        worksheet.SetTitle(dto.Title);
        worksheet.SetVersion(dto.Version);
        worksheet.SetPublished(dto.Published);
        worksheet.SetReportingFields(dto.ReportKeys, dto.ReportColumns, dto.ReportViewName);

        await DeleteExistingWorksheetChildrenAsync(worksheet);
        worksheet.Sections.Clear();

        return worksheet;
    }

    private async Task InsertWorksheetChildrenAsync(Worksheet worksheet, CreateWorksheetDto dto)
    {
        foreach (var section in dto.Sections.OrderBy(s => s.Order))
        {
            var worksheetSection = new WorksheetSection(Guid.NewGuid(), section.Name).SetOrder(section.Order);
            worksheetSection.WorksheetId = worksheet.Id;
            worksheetSection.Worksheet = worksheet;
            await worksheetSectionRepository.InsertAsync(worksheetSection);

            foreach (var field in section.Fields)
            {
                var customField = new CustomField(
                    Guid.NewGuid(),
                    field.Key,
                    worksheet.Name,
                    field.Label,
                    field.Type,
                    GetDefinitionText(field.Definition));
                customField.Section = worksheetSection;
                await customFieldRepository.InsertAsync(customField);
            }
        }
    }

    private static string GetDefinitionText(object? definition)
    {
        return definition switch
        {
            null => "{}",
            JsonElement { ValueKind: JsonValueKind.String } element => element.GetString() ?? "{}",
            JsonElement element => element.GetRawText(),
            string text => text,
            _ => JsonSerializer.Serialize(definition)
        };
    }

    private async Task DeleteExistingWorksheetChildrenAsync(Worksheet worksheet)
    {
        foreach (var section in worksheet.Sections)
        {
            await customFieldRepository.DeleteManyAsync(section.Fields.Select(field => field.Id));
        }

        await worksheetSectionRepository.DeleteManyAsync(worksheet.Sections.Select(section => section.Id));
    }

    private async Task UpsertWorksheetLinkAsync(Guid worksheetId, Guid correlationId)
    {
        var existingLink = await worksheetLinkRepository.GetExistingLinkAsync(worksheetId, correlationId, CorrelationConsts.FormVersion);
        if (existingLink != null)
        {
            existingLink.SetAnchor(FlexConsts.CustomTab).SetOrder(1);
            await worksheetLinkRepository.UpdateAsync(existingLink);
            return;
        }

        await worksheetLinkRepository.InsertAsync(new WorksheetLink(
            Guid.NewGuid(),
            worksheetId,
            correlationId,
            CorrelationConsts.FormVersion,
            FlexConsts.CustomTab,
            1));
    }
}
