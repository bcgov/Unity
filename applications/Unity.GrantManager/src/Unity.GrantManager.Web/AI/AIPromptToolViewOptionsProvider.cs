using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Web.AI
{
    public class AIPromptToolViewOptionsProvider(
        IWebHostEnvironment webHostEnvironment,
        IConfiguration configuration) : IAIPromptToolViewOptionsProvider, ITransientDependency
    {
        public bool IsDevPromptControlsEnabled =>
            string.Equals(webHostEnvironment.EnvironmentName, "Development", StringComparison.OrdinalIgnoreCase);

        public string DefaultPromptVersion =>
            string.IsNullOrWhiteSpace(configuration["Azure:OpenAI:PromptVersion"])
                ? "v1"
                : configuration["Azure:OpenAI:PromptVersion"]!.Trim().ToLowerInvariant();
    }
}
