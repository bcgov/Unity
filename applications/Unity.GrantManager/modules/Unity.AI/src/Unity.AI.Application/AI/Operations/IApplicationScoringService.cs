using System;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.AI.Operations
{
    public interface IApplicationScoringService
    {
        Task<string> RegenerateAndSaveAsync(ApplicationScoringOperationInputDto input, CancellationToken cancellationToken = default);
    }
}
