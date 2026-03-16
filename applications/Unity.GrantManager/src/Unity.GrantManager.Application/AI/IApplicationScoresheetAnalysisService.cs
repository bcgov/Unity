using System;
using System.Threading.Tasks;

namespace Unity.GrantManager.AI
{
    public interface IApplicationScoresheetAnalysisService
    {
        Task<string> RegenerateAndSaveAsync(Guid applicationId, string? promptVersion = null, bool capturePromptIo = false);
    }
}
