using System.Collections.Generic;
using Unity.GrantManager.AI;

namespace Unity.GrantManager.AI
{
    public interface IAIPromptIoCaptureStore
    {
        void Save(AIPromptIoCaptureResponse capture);
        IReadOnlyList<AIPromptIoCaptureResponse> GetRecent(string contextId, string promptType, string? promptVersion = null, int maxResults = 20);
    }
}
