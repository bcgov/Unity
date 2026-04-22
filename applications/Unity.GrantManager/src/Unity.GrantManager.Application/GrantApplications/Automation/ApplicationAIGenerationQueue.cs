using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.AI.Automation;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.GrantApplications.Automation.BackgroundJobs;
using Medallion.Threading;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.GrantApplications.Automation;

public class ApplicationAIGenerationQueue(
    IBackgroundJobManager backgroundJobManager,
    IRepository<AIGenerationRequest, Guid> generationRequestRepository,
    IDistributedLockProvider distributedLockProvider)
    : IApplicationAIGenerationQueue, ITransientDependency
{
    public async Task QueueAttachmentSummaryAsync(Guid applicationId, Guid? tenantId, string? promptVersion = null)
    {
        var requestKey = AIGenerationRequestKeyHelper.BuildRequestKey(tenantId, applicationId, AIGenerationRequestKeyHelper.AttachmentSummaryOperationType);
        await EnsureRequestAndEnqueueAsync(
            requestKey,
            tenantId,
            AIGenerationRequestKeyHelper.AttachmentSummaryOperationType,
            applicationId,
            () =>
            {
                return backgroundJobManager.EnqueueAsync(new GenerateAttachmentSummaryBackgroundJobArgs
                {
                    ApplicationId = applicationId,
                    PromptVersion = promptVersion,
                    TenantId = tenantId,
                    RequestKey = requestKey
                });
            });
    }

    public async Task QueueApplicationAnalysisAsync(Guid applicationId, Guid? tenantId, string? promptVersion = null)
    {
        var requestKey = AIGenerationRequestKeyHelper.BuildRequestKey(tenantId, applicationId, AIGenerationRequestKeyHelper.ApplicationAnalysisOperationType);
        await EnsureRequestAndEnqueueAsync(
            requestKey,
            tenantId,
            AIGenerationRequestKeyHelper.ApplicationAnalysisOperationType,
            applicationId,
            () =>
            {
                return backgroundJobManager.EnqueueAsync(new GenerateApplicationAnalysisBackgroundJobArgs
                {
                    ApplicationId = applicationId,
                    PromptVersion = promptVersion,
                    TenantId = tenantId,
                    RequestKey = requestKey
                });
            });
    }

    public async Task QueueApplicationScoringAsync(Guid applicationId, Guid? tenantId, string? promptVersion = null)
    {
        var requestKey = AIGenerationRequestKeyHelper.BuildRequestKey(tenantId, applicationId, AIGenerationRequestKeyHelper.ApplicationScoringOperationType);
        await EnsureRequestAndEnqueueAsync(
            requestKey,
            tenantId,
            AIGenerationRequestKeyHelper.ApplicationScoringOperationType,
            applicationId,
            () =>
            {
                return backgroundJobManager.EnqueueAsync(new GenerateApplicationScoringBackgroundJobArgs
                {
                    ApplicationId = applicationId,
                    PromptVersion = promptVersion,
                    TenantId = tenantId,
                    RequestKey = requestKey
                });
            });
    }

    public async Task QueueAllAIStagesAsync(Guid applicationId, Guid? tenantId, string? promptVersion = null)
    {
        var requestKey = AIGenerationRequestKeyHelper.BuildRequestKey(tenantId, applicationId, AIGenerationRequestKeyHelper.PipelineOperationType);
        await EnsureRequestAndEnqueueAsync(
            requestKey,
            tenantId,
            AIGenerationRequestKeyHelper.PipelineOperationType,
            applicationId,
            () =>
            {
                return backgroundJobManager.EnqueueAsync(new RunApplicationAIPipelineJobArgs
                {
                    ApplicationId = applicationId,
                    PromptVersion = promptVersion,
                    TenantId = tenantId,
                    RequestKey = requestKey
                });
            });
    }

    private async Task EnsureRequestAndEnqueueAsync(
        string requestKey,
        Guid? tenantId,
        string operationType,
        Guid? applicationId,
        Func<Task> enqueue)
    {
        var requestLock = distributedLockProvider.CreateLock($"ai-generation:{requestKey}");

        using (await requestLock.AcquireAsync())
        {
            var query = await generationRequestRepository.GetQueryableAsync();
            var existingRequests = query.Where(x =>
                x.RequestKey == requestKey
                && (x.Status == AIGenerationRequestStatus.Queued || x.Status == AIGenerationRequestStatus.Running));

            var existing = existingRequests
                .OrderByDescending(x => x.CreationTime)
                .ThenByDescending(x => x.Id)
                .FirstOrDefault();

            if (existing != null)
            {
                return;
            }

            var request = new AIGenerationRequest(
                Guid.NewGuid(),
                tenantId,
                operationType,
                applicationId,
                requestKey);

            await generationRequestRepository.InsertAsync(request, autoSave: true);
            await enqueue();
        }
    }
}
