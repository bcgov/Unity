using System;
using System.Threading.Tasks;

namespace Unity.GrantManager.AI
{
    public interface IApplicationScoringService
    {
        Task<string> RegenerateAndSaveAsync(Guid applicationId, string? promptVersion = null);
    }
}

