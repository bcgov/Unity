using System;
using System.Globalization;
using System.Threading.Tasks;
using Medallion.Threading;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Volo.Abp;
using Volo.Abp.DependencyInjection;

namespace Unity.AI.Cooldown;

public interface IAICooldownService
{
    Task<int> GetRemainingSecondsAsync(Guid userId);
    Task EnsureAsync(Guid? userId);
    Task StampAsync(Guid? userId);
}

public class AICooldownService(
    IDistributedCache cache,
    IConfiguration configuration,
    IDistributedLockProvider distributedLockProvider)
    : IAICooldownService, ITransientDependency
{
    public async Task<int> GetRemainingSecondsAsync(Guid userId)
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

    public async Task EnsureAsync(Guid? userId)
    {
        if (userId is not Guid resolvedUserId)
        {
            return;
        }

        var userLock = distributedLockProvider.CreateLock(CooldownLockPrefix + resolvedUserId);
        using (await userLock.AcquireAsync())
        {
            var remaining = await GetRemainingSecondsAsync(resolvedUserId);
            if (remaining > 0)
            {
                throw new UserFriendlyException(
                    $"AI generation is rate limited. Try again in {remaining} second{(remaining == 1 ? "" : "s")}.");
            }
        }
    }

    public async Task StampAsync(Guid? userId)
    {
        if (userId is Guid resolvedUserId)
        {
            var userLock = distributedLockProvider.CreateLock(CooldownLockPrefix + resolvedUserId);
            using (await userLock.AcquireAsync())
            {
                await StampAsync(resolvedUserId, CooldownSeconds);
            }
        }
    }

    private const string CooldownKeyPrefix = "ai-generation:cooldown:";
    private const string CooldownLockPrefix = "ai-generation:cooldown-lock:";
    private const string CooldownSecondsConfigurationKey = "Azure:Generation:CooldownSeconds";

    private int CooldownSeconds
    {
        get
        {
            var configured = configuration.GetValue<int?>(CooldownSecondsConfigurationKey);
            if (configured is > 0)
            {
                return configured.Value;
            }

            throw new AbpException($"{CooldownSecondsConfigurationKey} must be configured with a positive value.");
        }
    }

    private async Task StampAsync(Guid userId, int seconds)
    {
        var until = DateTime.UtcNow.AddSeconds(seconds);
        await cache.SetStringAsync(
            KeyFor(userId),
            until.Ticks.ToString(CultureInfo.InvariantCulture),
            new DistributedCacheEntryOptions { AbsoluteExpiration = until });
    }

    private static string KeyFor(Guid userId) => CooldownKeyPrefix + userId;
}
