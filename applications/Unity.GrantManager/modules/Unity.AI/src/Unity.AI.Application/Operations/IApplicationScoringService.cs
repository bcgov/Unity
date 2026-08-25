using System;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.AI.Operations
{
    public interface IApplicationScoringService
    {
        Task<string> RegenerateAsync(ApplicationScoringOperationInputDto input, CancellationToken cancellationToken = default);
    }
}
