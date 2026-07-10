using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.AI;
using Unity.AI.Domain;
using Unity.AI.Requests;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Flex;
using Unity.Flex.Domain.WorksheetLinks;
using Unity.Flex.Domain.Worksheets;
using Unity.Flex.Worksheets;
using Unity.Modules.Shared.Correlation;
using Unity.AI.RateLimit;
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
    IAIService aiService,
    IRepository<AIGenerationRequest, Guid> generationRequestRepository,
    IRepository<AIOperation, Guid> operationRepository,
    ICurrentTenant currentTenant,
    IUnitOfWorkManager unitOfWorkManager,
    IAIRateLimiter aiRateLimiter,
    ILogger<GenerateFormWorksheetJob> logger) : AsyncBackgroundJob<GenerateFormWorksheetBackgroundJobArgs>, ITransientDependency
{
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
                operationRepository,
                args.TenantId,
                args.ApplicationId,
                AIGenerationRequestKeyHelper.FormWorksheetOperationType);
            try
            {
                var formVersion = await applicationFormVersionRepository.GetAsync(args.ApplicationFormVersionId);
                var applicationForm = await applicationFormRepository.GetAsync(formVersion.ApplicationFormId);
                var worksheetName = BuildWorksheetName(formVersion.Id, applicationForm.Id);
                var existingWorksheet = await worksheetRepository.GetByNameAsync(worksheetName, true)
                    ?? (await worksheetRepository.GetListOrderedAsync(formVersion.Id, CorrelationConsts.FormVersion, includeDetails: true)).FirstOrDefault();

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

                var worksheetJson = await aiService.GenerateFormWorksheetAsync(new MappingSuggestionRequest
                {
                    Data = JsonSerializer.SerializeToElement(promptData),
                    PromptVersion = args.PromptVersion
                });

                var createDto = ParseWorksheetDefinition(worksheetJson);
                var worksheet = existingWorksheet ?? await worksheetRepository.InsertAsync(BuildWorksheet(createDto, worksheetName));
                RebuildWorksheet(worksheet, createDto);
                worksheet.SetPublished(true);
                await worksheetRepository.UpdateAsync(worksheet);

                await UpsertWorksheetLinkAsync(worksheet.Id, formVersion.Id);

                await AIGenerationRequestJobHelper.StampRateLimitBestEffortAsync(aiRateLimiter, logger, args.RequestedByUserId, args.ApplicationId, AIGenerationRequestKeyHelper.FormWorksheetOperationType);
                await AIGenerationRequestJobHelper.MarkCompletedInNewUowAsync(
                    unitOfWorkManager,
                    generationRequestRepository,
                    operationRepository,
                    args.TenantId,
                    args.ApplicationId,
                    AIGenerationRequestKeyHelper.FormWorksheetOperationType);
            }
            catch (Exception ex)
            {
                await AIGenerationRequestJobHelper.MarkFailedInNewUowAsync(
                    unitOfWorkManager,
                    generationRequestRepository,
                    operationRepository,
                    args.TenantId,
                    args.ApplicationId,
                    AIGenerationRequestKeyHelper.FormWorksheetOperationType,
                    ex.Message);
                throw;
            }
        }
    }

    private static CreateWorksheetDto ParseWorksheetDefinition(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException("Worksheet generation returned empty content.");
        }

        var dto = JsonSerializer.Deserialize<CreateWorksheetDto>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

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
                    field.Definition);
                customField.Section = worksheetSection;
                worksheetSection.Fields.Add(customField);
            }
        }

        return worksheet;
    }

    private static void RebuildWorksheet(Worksheet worksheet, CreateWorksheetDto dto)
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
                worksheetSection.Fields.Add(customField);
            }
        }
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
