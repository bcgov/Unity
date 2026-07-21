using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
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
    IConfiguration configuration,
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

                if (existingWorksheet == null || existingWorksheet.Published)
                {
                    var promptData = new
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

                    var useMock = configuration.GetValue<bool>("AI:FormWorksheet:UseMock");
                    var worksheetResponse = useMock
                        ? CreateMockWorksheetResponse()
                        : await aiService.GenerateFormWorksheetAsync(new FormWorksheetRequest
                        {
                            Data = JsonSerializer.SerializeToElement(promptData),
                            PromptVersion = args.PromptVersion
                        });

                    if (useMock)
                    {
                        logger.LogWarning("Using mock AI worksheet data for form version {FormVersionId}.", formVersion.Id);
                    }

                    var createDto = ParseWorksheetDefinition(worksheetResponse.Worksheet);
                    if (existingWorksheet == null)
                    {
                        var worksheet = BuildWorksheet(createDto, worksheetName);
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

                        RebuildWorksheet(existingWorksheet, createDto);
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

    private static FormWorksheetResponse CreateMockWorksheetResponse() => new()
    {
        Worksheet = """
        {
          "title": "AI Suggested Worksheet (Mock)",
          "version": 1,
          "published": false,
          "sections": [
            {
              "name": "Suggested Fields",
              "order": 1,
              "fields": [
                {
                  "key": "projectName",
                  "label": "Project Name",
                  "type": 2,
                  "order": 1,
                  "definition": { "required": false }
                },
                {
                  "key": "projectSummary",
                  "label": "Project Summary",
                  "type": 14,
                  "order": 2,
                  "definition": { "required": false }
                },
                {
                  "key": "requestedAmount",
                  "label": "Requested Amount",
                  "type": 5,
                  "order": 3,
                  "definition": { "required": false }
                }
              ]
            }
          ]
        }
        """
    };

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

    private static void RebuildWorksheet(Worksheet worksheet, CreateWorksheetDto dto)
    {
        worksheet.SetTitle(dto.Title);
        worksheet.SetVersion(dto.Version);
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
    }

}
