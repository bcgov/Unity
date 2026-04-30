using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Unity.AI.RateLimit;

[Authorize]
public class AIRateLimitAppService(IAIRateLimiter rateLimiter)
    : AIAppService, IAIRateLimitAppService
{
    public virtual Task<AIRateLimitStateDto> GetStateAsync() => rateLimiter.GetStateAsync();
}
