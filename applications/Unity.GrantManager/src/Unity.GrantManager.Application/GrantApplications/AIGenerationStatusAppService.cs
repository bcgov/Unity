using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.GrantApplications;

public class AIGenerationStatusAppService(
    IRepository<AIGenerationRequest, Guid> generationRequestRepository,
    IRepository<AIOperation, Guid> operationRepository,
    ICurrentTenant currentTenant)
    : ApplicationService, IAIGenerationStatusAppService
{
    public virtual async Task<AIGenerationRequestDto?> GetLatestAsync(Guid applicationId, string operationType, Guid? tenantId = null)
    {
        var operationName = AIGenerationRequestKeyHelper.ResolveOperationName(operationType);

        if (operationName == null)
        {
            return null;
        }

        var operation = await ResolveOperationAsync(operationName);
        if (operation == null)
        {
            return null;
        }

        var query = await generationRequestRepository.GetQueryableAsync();
        var resolvedTenantId = tenantId ?? currentTenant.Id;

        var item = query
            .Where(x =>
                x.ApplicationId == applicationId &&
                x.OperationId == operation.Id &&
                x.TenantId == resolvedTenantId)
            .OrderByDescending(x => x.CreationTime)
            .ThenByDescending(x => x.Id)
            .FirstOrDefault();

        return item == null
            ? null
            : new AIGenerationRequestDto
            {
                Id = item.Id,
                ApplicationId = item.ApplicationId,
                OperationId = item.OperationId,
                OperationType = operationType,
                Status = item.Status,
                StartedAt = item.StartedAt,
                CompletedAt = item.CompletedAt,
                FailureReason = item.FailureReason,
                IsActive = item.IsActive
            };
    }

    private async Task<AIOperation?> ResolveOperationAsync(string operationName)
    {
        var operations = await operationRepository.GetQueryableAsync();
        return operations.FirstOrDefault(operation => operation.Name == operationName);
    }
}
