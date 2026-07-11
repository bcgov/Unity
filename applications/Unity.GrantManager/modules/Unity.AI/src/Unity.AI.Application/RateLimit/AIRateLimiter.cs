using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Users;

namespace Unity.AI.RateLimit;

[Authorize]
[Route("api/app/ai/cooldown")]
public class AICooldownAppService(
    ICurrentUser currentUser,
    IAICooldownService aiCooldownService)
    : AIAppService, IAICooldownAppService, ITransientDependency
{
    private const string CooldownKeyPrefix = "ai-generation:cooldown:";
    private const string CooldownLockPrefix = "ai-generation:cooldown-lock:";
    private const string CooldownSecondsConfigurationKey = "Azure:Generation:CooldownSeconds";

    private int CooldownSeconds
    {
        get
        {
            RetryAfterSeconds = await aiCooldownService.GetRemainingSecondsAsync(userId),
        };
    }

    public virtual async Task EnsureAsync()
    {
        await aiCooldownService.EnsureAsync(currentUser.Id);
    }

    public virtual async Task StampAsync()
    {
        await StampAsync(currentUser.Id);
    }

    public virtual async Task StampAsync(Guid? userId)
    {
        await aiCooldownService.StampAsync(userId);
    }
}
