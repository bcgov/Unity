using System.Threading;
using System.Threading.Tasks;

namespace Unity.AI.Runtime;

public interface IAIPromptTemplateStore
{
    Task<AIPromptTemplateSnapshot> GetRequiredPromptAsync(
        string promptType,
        string promptVersion,
        CancellationToken cancellationToken = default);
}
