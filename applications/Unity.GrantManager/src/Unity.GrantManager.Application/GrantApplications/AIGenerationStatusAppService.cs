using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.GrantApplications;

public class AIGenerationStatusAppService(
    IRepository<AIGenerationRequest, Guid> generationRequestRepository,
    ICurrentTenant currentTenant)
    : ApplicationService, IAIGenerationStatusAppService
{
    public virtual async Task<AIGenerationRequestDto?> GetLatestAsync(Guid applicationId, string operationType, Guid? tenantId = null)
    {
        var query = await generationRequestRepository.GetQueryableAsync();
        var resolvedTenantId = tenantId ?? currentTenant.Id;

        var item = query
            .Where(x =>
                x.ApplicationId == applicationId &&
                x.OperationType == operationType &&
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
                OperationType = item.OperationType,
                RequestKey = item.RequestKey,
                Status = item.Status,
                StartedAt = item.StartedAt,
                CompletedAt = item.CompletedAt,
                FailureReason = item.FailureReason,
                IsActive = item.IsActive
            };
    }
}
