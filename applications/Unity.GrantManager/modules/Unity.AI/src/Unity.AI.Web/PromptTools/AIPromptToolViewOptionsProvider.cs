using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using Volo.Abp.DependencyInjection;

namespace Unity.AI.Web.PromptTools;

public class AIPromptToolViewOptionsProvider(
    IWebHostEnvironment webHostEnvironment,
    IConfiguration configuration) : IAIPromptToolViewOptionsProvider, ITransientDependency
{
    public bool IsDevPromptControlsEnabled =>
        string.Equals(webHostEnvironment.EnvironmentName, "Development", StringComparison.OrdinalIgnoreCase);

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
