using System.Threading;
using System.Threading.Tasks;

namespace Unity.AI.Runtime.Execution;

public interface IAIPromptTemplateProvider
{
    Task<AIPromptTemplateSnapshot> GetRequiredPromptAsync(
        string promptType,
        string promptVersion,
        CancellationToken cancellationToken = default);
}
