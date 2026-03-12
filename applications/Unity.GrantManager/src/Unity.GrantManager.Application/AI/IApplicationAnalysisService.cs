using System;
using System.Threading.Tasks;

namespace Unity.GrantManager.AI
{
    public interface IApplicationAnalysisService
    {
        Task<string> RegenerateAndSaveAsync(Guid applicationId, string? promptVersion = null, bool capturePromptIo = false);
    }
}
