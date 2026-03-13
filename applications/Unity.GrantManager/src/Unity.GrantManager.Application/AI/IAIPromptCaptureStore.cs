using System.Collections.Generic;

namespace Unity.GrantManager.AI
{
    public interface IAIPromptCaptureStore
    {
        void Save(AIPromptCaptureResponse capture);
        IReadOnlyList<AIPromptCaptureResponse> GetRecent(string contextId, string promptType, string? promptVersion = null, int maxResults = 20);
    }
}
