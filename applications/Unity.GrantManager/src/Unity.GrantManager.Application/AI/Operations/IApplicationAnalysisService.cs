using System;
using System.Threading.Tasks;

namespace Unity.GrantManager.AI.Operations
{
    public interface IApplicationAnalysisService
    {
        Task<string> RegenerateAndSaveAsync(Guid applicationId, string? promptVersion = null);
    }
}
