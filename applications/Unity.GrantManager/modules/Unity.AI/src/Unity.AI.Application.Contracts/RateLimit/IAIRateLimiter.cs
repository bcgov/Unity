using System.Threading.Tasks;

namespace Unity.AI.RateLimit;

public interface IAIRateLimiter
{
    /// <summary>
    /// Throws <see cref="Volo.Abp.UserFriendlyException"/> if the current user is still
    /// inside their AI generate cooldown window, otherwise stamps a fresh cooldown.
    /// </summary>
    Task EnsureAndStampAsync();

    /// <summary>
    /// Returns the remaining cooldown for the current user. RetryAfterSeconds is 0 when
    /// the user can generate immediately.
    /// </summary>
    Task<AIRateLimitStateDto> GetStateAsync();
}
