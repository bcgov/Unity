using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.AI;
using Unity.AI.Requests;
using Unity.AI.Responses;
using Unity.GrantManager.ApplicationForms;
using Unity.AI.RateLimit;
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
    IAIService aiService,
    IRepository<ApplicationFormVersion, Guid> applicationFormVersionRepository,
    IRepository<AIGenerationRequest, Guid> generationRequestRepository,
    IRepository<AIOperation, Guid> operationRepository,
    ICurrentTenant currentTenant,
    IUnitOfWorkManager unitOfWorkManager,
    IAIRateLimiter aiRateLimiter,
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
                operationRepository,
                args.TenantId,
                args.ApplicationId,
                AIGenerationRequestKeyHelper.FormMappingOperationType);
            try
            {
                var readModel = await mappingReadService.GetAsync(args.ApplicationFormVersionId);
                var response = await aiService.GenerateFormMappingAsync(new MappingSuggestionRequest
                {
                    Data = JsonSerializer.SerializeToElement(readModel),
                    PromptVersion = args.PromptVersion
                });

                var submissionHeaderMapping = MappingSuggestionResponseMapper.BuildSubmissionHeaderMapping(response);
                var applicationFormVersion = await applicationFormVersionRepository.GetAsync(args.ApplicationFormVersionId);
                applicationFormVersion.SubmissionHeaderMapping = JsonSerializer.Serialize(submissionHeaderMapping);
                await applicationFormVersionRepository.UpdateAsync(applicationFormVersion, true);

                await AIGenerationRequestJobHelper.StampRateLimitBestEffortAsync(aiRateLimiter, logger, args.RequestedByUserId, args.ApplicationId, AIGenerationRequestKeyHelper.FormMappingOperationType);
                await AIGenerationRequestJobHelper.MarkCompletedInNewUowAsync(
                    unitOfWorkManager,
                    generationRequestRepository,
                    operationRepository,
                    args.TenantId,
                    args.ApplicationId,
                    AIGenerationRequestKeyHelper.FormMappingOperationType);
            }
            catch (System.Exception ex)
            {
                await AIGenerationRequestJobHelper.MarkFailedInNewUowAsync(
                    unitOfWorkManager,
                    generationRequestRepository,
                    operationRepository,
                    args.TenantId,
                    args.ApplicationId,
                    AIGenerationRequestKeyHelper.FormMappingOperationType,
                    ex.Message);
                throw;
            }
        }
    }

}
