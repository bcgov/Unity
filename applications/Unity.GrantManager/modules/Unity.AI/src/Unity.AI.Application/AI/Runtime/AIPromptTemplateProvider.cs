using System.Threading;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace Unity.AI.Runtime;

public class AIPromptTemplateProvider(
    IAIPromptTemplateStore promptTemplateStore) : IAIPromptTemplateProvider, ITransientDependency
{
    public async Task<AIPromptTemplateSnapshot> GetRequiredPromptAsync(
        string promptType,
        string promptVersion,
        CancellationToken cancellationToken = default)
    {
        return await promptTemplateStore.GetRequiredPromptAsync(promptType, promptVersion, cancellationToken);
    }
}
