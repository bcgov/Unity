using System;
using System.Globalization;
using System.Threading.Tasks;
using Medallion.Threading;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Users;

namespace Unity.AI.RateLimit;

/// <summary>
/// Per-user cooldown for AI generate calls. KISS: a single cache entry per user
/// holds the cooldown end ticks; the cache TTL matches the cooldown so a missing
/// entry means the user can generate again. Anonymous/system callers are not
/// rate-limited (background event handlers also flow through the AI queue).
/// </summary>
public class AIRateLimiter(
    IDistributedCache cache,
    ICurrentUser currentUser,
    IConfiguration configuration,
    IDistributedLockProvider distributedLockProvider) : IAIRateLimiter, ITransientDependency
{
    private const string KeyPrefix = "AI:Cooldown:";
    private const string LockPrefix = "AI:CooldownLock:";
    private const int DefaultCooldownSeconds = 60;

    private int CooldownSeconds =>
        configuration.GetValue<int?>("AI:RateLimit:CooldownSeconds") ?? DefaultCooldownSeconds;

    public virtual async Task EnsureAsync()
    {
        if (currentUser.Id is not Guid userId)
        {
            // No user (background/system flow). User-level rate limit does not apply.
            return;
        }

        var userLock = distributedLockProvider.CreateLock(LockPrefix + userId);
        using (await userLock.AcquireAsync())
        {
            var remaining = await GetRemainingSecondsAsync(userId);
            if (remaining > 0)
            {
                throw new UserFriendlyException(
                    $"AI generation is rate limited. Try again in {remaining} second{(remaining == 1 ? "" : "s")}.");
            }
        }
    }

    public virtual async Task StampAsync()
    {
        await StampAsync(currentUser.Id);
    }

    public virtual async Task StampAsync(Guid? userId)
    {
        if (userId is Guid resolvedUserId)
        {
            await StampAsync(resolvedUserId, CooldownSeconds);
        }
    }

    public virtual async Task<AIRateLimitStateDto> GetStateAsync()
    {
        if (currentUser.Id is not Guid userId)
        {
            return new AIRateLimitStateDto { RetryAfterSeconds = 0 };
        }

        return new AIRateLimitStateDto
        {
            RetryAfterSeconds = await GetRemainingSecondsAsync(userId)
        };
    }

    private async Task<int> GetRemainingSecondsAsync(Guid userId)
    {
        var raw = await cache.GetStringAsync(KeyFor(userId));
        if (string.IsNullOrEmpty(raw) ||
            !long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var untilTicks) ||
            untilTicks < DateTime.MinValue.Ticks ||
            untilTicks > DateTime.MaxValue.Ticks)
        {
            return 0;
        }

        var seconds = (int)Math.Ceiling((new DateTime(untilTicks, DateTimeKind.Utc) - DateTime.UtcNow).TotalSeconds);
        return seconds > 0 ? seconds : 0;
    }

    private async Task StampAsync(Guid userId, int seconds)
    {
        var until = DateTime.UtcNow.AddSeconds(seconds);
        await cache.SetStringAsync(
            KeyFor(userId),
            until.Ticks.ToString(CultureInfo.InvariantCulture),
            new DistributedCacheEntryOptions { AbsoluteExpiration = until });
    }

    private static string KeyFor(Guid userId) => KeyPrefix + userId;
}
