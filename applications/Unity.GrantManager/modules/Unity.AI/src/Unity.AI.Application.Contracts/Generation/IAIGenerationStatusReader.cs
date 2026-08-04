using System;
using System.Threading.Tasks;

namespace Unity.AI.Generation;

public interface IAIGenerationStatusReader
{
    Task<AIGenerationRequestDto?> GetLatestAsync(Guid applicationId, string operationType, Guid? tenantId = null);
}
