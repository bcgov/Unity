using System;
using System.Threading;
using System.Threading.Tasks;

namespace Unity.AI.Operations
{
    public interface IApplicationAnalysisService
    {
        Task<string> RegenerateAndSaveAsync(ApplicationAnalysisOperationInputDto input, CancellationToken cancellationToken = default);
    }
}
