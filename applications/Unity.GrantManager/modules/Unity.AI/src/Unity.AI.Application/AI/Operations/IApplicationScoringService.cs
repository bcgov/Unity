using System;
using System.Threading.Tasks;

namespace Unity.AI.Operations
{
    public interface IApplicationScoringService
    {
        Task<string> RegenerateAndSaveAsync(Guid applicationId, string? promptVersion = null);
    }
}
