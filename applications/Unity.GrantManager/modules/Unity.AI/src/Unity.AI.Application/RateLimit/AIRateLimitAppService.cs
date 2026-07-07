using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Unity.AI.RateLimit;

[Authorize]
[Route("api/app/ai/rate-limit")]
public class AIRateLimitAppService(IAIRateLimiter rateLimiter)
    : AIAppService, IAIRateLimitAppService
{
    [HttpGet("state")]
    public virtual Task<AIRateLimitStateDto> GetStateAsync() => rateLimiter.GetStateAsync();
}
