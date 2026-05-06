using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Unity.Modules.Shared.Permissions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Security.Claims;

namespace Unity.AI.Web.PromptTools;

public class AIPromptToolAccessProvider(
    IAuthorizationService authorizationService,
    ICurrentPrincipalAccessor currentPrincipalAccessor,
    IConfiguration configuration) : IAIPromptToolAccessProvider, ITransientDependency
{
    public async Task<bool> CanViewPromptToolsAsync()
    {
        var principal = currentPrincipalAccessor.Principal;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var authorizationResult = await authorizationService.AuthorizeAsync(
            principal,
            IdentityConsts.ITOperationsPolicyName);

        return authorizationResult.Succeeded;
    }

    public string DefaultPromptVersion
    {
        get
        {
            var configuredPromptVersion = configuration["Azure:Operations:Defaults:PromptVersion"];
            if (string.IsNullOrWhiteSpace(configuredPromptVersion))
            {
                configuredPromptVersion = configuration["Azure:OpenAI:PromptVersion"];
            }

            return string.IsNullOrWhiteSpace(configuredPromptVersion)
                ? "v1"
                : configuredPromptVersion.Trim().ToLowerInvariant();
        }
    }
}
