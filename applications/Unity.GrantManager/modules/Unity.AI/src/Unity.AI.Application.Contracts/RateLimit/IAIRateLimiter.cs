using System.Threading.Tasks;
using System;

namespace Unity.AI.RateLimit;

public interface IAIRateLimiter
{
    /// <summary>
    /// Throws <see cref="Volo.Abp.UserFriendlyException"/> if the current user is still
    /// inside their AI generate cooldown window.
    /// </summary>
    Task EnsureAsync();

    /// <summary>
    /// Starts a fresh cooldown for the current user. No-op for callers without a user.
    /// </summary>
    Task StampAsync();

    /// <summary>
    /// Starts a fresh cooldown for the supplied user. No-op when userId is null.
    /// </summary>
    Task StampAsync(Guid? userId);

    /// <summary>
    /// Returns the current user's shared AI generation state. RetryAfterSeconds is 0 when
    /// the user can generate immediately, unless a generation is still active.
    /// </summary>
    Task<AIRateLimitStateDto> GetStateAsync();
}
