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
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.ApplicationForms.Mapping;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Flex;
using Unity.Flex.Domain.WorksheetLinks;
using Unity.Flex.Domain.Worksheets;
using Unity.Flex.Worksheets;
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

                List<Worksheet> worksheetSnapshots = [];
                if (existingWorksheet != null)
                {
                    worksheetSnapshots.Add(existingWorksheet);
                }
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
                    existingWorksheets = worksheetSnapshots.Select(worksheet => new
                    {
                        worksheet.Id,
                        worksheet.Name,
                        worksheet.Title,
                        worksheet.Version,
                        worksheet.Published,
                        worksheet.ReportViewName,
                        sections = worksheet.Sections.Select(section => new
                        {
                            section.Name,
                            section.Order,
                            fields = section.Fields.Select(field => new
                            {
                                field.Name,
                                field.Key,
                                field.Label,
                                field.Type,
                                field.Order,
                                field.Enabled,
                                field.Definition
                            })
                        })
                    })
                };

                var worksheetResponse = await aiService.GenerateFormWorksheetAsync(new FormWorksheetRequest
                {
                    Data = JsonSerializer.SerializeToElement(promptData),
                    PromptVersion = args.PromptVersion
                });

                var worksheetJson = worksheetResponse.Worksheet;
                var createDto = ParseWorksheetDefinition(worksheetJson);
                var worksheet = existingWorksheet == null
                    ? BuildWorksheet(createDto, worksheetName)
                    : RebuildWorksheet(existingWorksheet, createDto);
                worksheet.SetPublished(true);
                if (existingWorksheet == null)
                {
                    await worksheetRepository.InsertAsync(worksheet);
                }
                else
                {
                    await worksheetRepository.UpdateAsync(worksheet);
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

    internal static CreateWorksheetDto ParseWorksheetDefinition(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("Worksheet generation returned empty content.");
        }

        var dto = JsonSerializer.Deserialize<CreateWorksheetDto>(json, CaseInsensitiveJsonOptions);

        if (dto == null || string.IsNullOrWhiteSpace(dto.Title) || dto.Sections is not { Count: > 0 })
        {
            throw new InvalidOperationException("Worksheet generation returned an unusable worksheet definition.");
        }

        return dto;
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
                    field.Definition);
                customField.Section = worksheetSection;
                worksheetSection.AddField(customField);
            }
        }

        return worksheet;
    }

    private static Worksheet RebuildWorksheet(Worksheet worksheet, CreateWorksheetDto dto)
    {
        worksheet.SetName(worksheet.Name);
        worksheet.SetTitle(dto.Title);
        worksheet.SetVersion(dto.Version);
        worksheet.SetPublished(dto.Published);
        worksheet.SetReportingFields(dto.ReportKeys, dto.ReportColumns, dto.ReportViewName);

        worksheet.Sections.Clear();

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
                    field.Definition);
                customField.Section = worksheetSection;
                worksheetSection.AddField(customField);
            }
        }

        return worksheet;
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
