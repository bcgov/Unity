using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.AI.Cooldown;
using Unity.AI.Operations;
using Unity.AI.Requests;
using Unity.AI.Responses;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.ApplicationForms.Mapping;
using Unity.GrantManager.Applications;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;

namespace Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;

public class GenerateFormMappingJob(
    IApplicationFormVersionMappingReadService mappingReadService,
    IFormMappingService aiService,
    IRepository<ApplicationFormVersion, Guid> applicationFormVersionRepository,
    IRepository<AIGenerationRequest, Guid> generationRequestRepository,
    ICurrentTenant currentTenant,
    IUnitOfWorkManager unitOfWorkManager,
    IAICooldownService aiCooldownService,
    ILogger<GenerateFormMappingJob> logger) : AsyncBackgroundJob<GenerateFormMappingBackgroundJobArgs>, ITransientDependency
{
    public override async Task ExecuteAsync(GenerateFormMappingBackgroundJobArgs args)
    {
        using var logScope = AIGenerationLogScope.Begin(
            logger,
            AIGenerationRequestKeyHelper.FormMappingOperationType,
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
                var readModel = await mappingReadService.GetAsync(args.ApplicationFormVersionId);
                var response = await aiService.GenerateFormMappingAsync(new FormMappingRequest
                {
                    Data = FormMappingPromptDataBuilder.Build(readModel),
                    PromptVersion = args.PromptVersion
                });

                var submissionHeaderMapping = FormMappingResponseMapper.BuildSubmissionHeaderMapping(response);
                var applicationFormVersion = await applicationFormVersionRepository.GetAsync(args.ApplicationFormVersionId);
                applicationFormVersion.SubmissionHeaderMapping = submissionHeaderMapping;
                await applicationFormVersionRepository.UpdateAsync(applicationFormVersion, true);

                await AIGenerationRequestJobHelper.StampCooldownBestEffortAsync(aiCooldownService, logger, args.RequestedByUserId, args.ApplicationId, AIGenerationRequestKeyHelper.FormMappingOperationType);
                await AIGenerationRequestJobHelper.MarkCompletedInNewUowAsync(
                    unitOfWorkManager,
                    generationRequestRepository,
                    args.TenantId,
                    args.ApplicationId,
                    args.OperationId);
            }
            catch (System.Exception ex)
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

}
