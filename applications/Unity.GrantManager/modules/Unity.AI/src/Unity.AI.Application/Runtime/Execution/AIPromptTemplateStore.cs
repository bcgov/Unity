using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.AI.Domain;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace Unity.AI.Runtime.Execution;

public class AIPromptTemplateStore(
    IRepository<AIPrompt, Guid> promptRepository,
    IDataFilter<IMultiTenant> multiTenantDataFilter,
    ICurrentTenant currentTenant) : IAIPromptTemplateStore, ITransientDependency
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
            var prompts = await promptRepository.GetListAsync(
                p => p.Name == promptType && p.VersionNumber == versionNumber && p.IsActive,
                cancellationToken: cancellationToken);
            var prompt = currentTenant.Id is Guid tenantId
                ? prompts.FirstOrDefault(p => p.TenantId == tenantId)
                : null;
            prompt ??= prompts.FirstOrDefault(p => p.TenantId == null);
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
