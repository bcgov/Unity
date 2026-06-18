using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace Unity.AI.Runtime;

public class AIPromptTemplateProvider(
    IRepository<AIPrompt, Guid> promptRepository,
    IRepository<AIPromptVersion, Guid> promptVersionRepository,
    ICurrentTenant currentTenant) : IAIPromptTemplateProvider, ITransientDependency
{
    public async Task<AIPromptTemplateSnapshot> GetRequiredPromptAsync(
        string promptType,
        string promptVersion,
        CancellationToken cancellationToken = default)
    {
        var normalizedPromptVersion = OpenAIPromptRenderer.ResolvePromptVersion(promptVersion);
        var versionNumber = OpenAIPromptRenderer.ResolvePromptVersionNumber(normalizedPromptVersion);

        using (currentTenant.Change(null))
        {
            var prompts = await promptRepository.GetListAsync(p => p.Name == promptType);
            var prompt = prompts.FirstOrDefault();
            if (prompt == null || !prompt.IsActive)
            {
                throw new InvalidOperationException($"AI prompt '{promptType}' is not configured.");
            }

            var versions = await promptVersionRepository.GetListAsync(
                version => version.PromptId == prompt.Id && version.VersionNumber == versionNumber);
            var version = versions.FirstOrDefault();
            if (version == null || !version.IsPublished || version.IsDeprecated)
            {
                throw new InvalidOperationException(
                    $"AI prompt version '{normalizedPromptVersion}' for prompt '{promptType}' is not configured.");
            }

            return new AIPromptTemplateSnapshot(
                normalizedPromptVersion,
                version.SystemPrompt,
                version.UserPromptTemplate,
                version.MetadataJson);
        }
    }
}
