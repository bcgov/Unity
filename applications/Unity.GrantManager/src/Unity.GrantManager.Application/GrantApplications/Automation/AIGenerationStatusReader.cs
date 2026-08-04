using System;
using System.Threading.Tasks;
using Unity.AI.Generation;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.GrantApplications.Automation;

public class AIGenerationStatusReader(
    IAIGenerationStatusAppService statusAppService) : IAIGenerationStatusReader, ITransientDependency
{
    public async Task<Unity.AI.Generation.AIGenerationRequestDto?> GetLatestAsync(
        Guid applicationId,
        string operationType,
        Guid? tenantId = null)
    {
        var status = await statusAppService.GetLatestAsync(applicationId, operationType, tenantId);
        return status == null
            ? null
            : new Unity.AI.Generation.AIGenerationRequestDto
            {
                Id = status.Id,
                ApplicationId = status.ApplicationId,
                OperationId = status.OperationId,
                OperationType = status.OperationType,
                Status = status.Status.ToString(),
                StartedAt = status.StartedAt,
                CompletedAt = status.CompletedAt,
                FailureReason = status.FailureReason,
                IsActive = status.IsActive
            };
    }
}
