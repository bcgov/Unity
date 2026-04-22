using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.GrantManager.GrantApplications;

public interface IAIGenerationStatusAppService : IApplicationService
{
    Task<AIGenerationRequestDto?> GetLatestAsync(Guid applicationId, string operationType, Guid? tenantId = null);
}
