using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace Unity.AI.RateLimit;

public interface IAIRateLimitAppService : IApplicationService
{
    Task<AIRateLimitStateDto> GetStateAsync();
}
