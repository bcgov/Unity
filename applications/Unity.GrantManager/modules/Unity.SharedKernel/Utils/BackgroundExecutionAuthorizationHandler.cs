using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

namespace Unity.Modules.Shared.Utils;

public static class BackgroundExecutionPolicy
{
    public const string Name = "Unity.BackgroundExecution";
}

public sealed class BackgroundExecutionRequirement : IAuthorizationRequirement;

public sealed class BackgroundExecutionAuthorizationHandler
    : AuthorizationHandler<BackgroundExecutionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        BackgroundExecutionRequirement requirement)
    {
        if (BackgroundJobExecutionContext.IsActive)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}