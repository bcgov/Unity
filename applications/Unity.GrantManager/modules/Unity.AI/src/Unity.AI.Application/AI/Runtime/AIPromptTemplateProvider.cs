using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace Unity.AI.Runtime;

public class AIPromptTemplateProvider(
    IRepository<AIPrompt, Guid> promptRepository,
    IDataFilter<IMultiTenant> multiTenantDataFilter) : IAIPromptTemplateProvider, ITransientDependency
{
    public async Task<AIPromptTemplateSnapshot> GetRequiredPromptAsync(
        string promptType,
        string promptVersion,
        CancellationToken cancellationToken = default)
    {
        var normalizedPromptVersion = OpenAIPromptRenderer.ResolvePromptVersion(promptVersion);
        var versionNumber = OpenAIPromptRenderer.ResolvePromptVersionNumber(normalizedPromptVersion);

        using (multiTenantDataFilter.Disable())
        {
            var prompt = await promptRepository.FindAsync(p =>
                p.TenantId == null && p.Name == promptType && p.VersionNumber == versionNumber);
            if (prompt == null || !prompt.IsActive)
            {
                throw new InvalidOperationException(
                    $"AI prompt '{promptType}' version '{normalizedPromptVersion}' is not configured.");
            }

            return new AIPromptTemplateSnapshot(
                normalizedPromptVersion,
                prompt.SystemPrompt,
                prompt.UserPrompt,
                prompt.MetadataJson);
        }
    }
}
