using System.Threading.Tasks;
using Unity.AI.Models;

namespace Unity.AI.Runtime;

internal interface IOpenAITransportService
{
    Task<AIOperationResult> GenerateSummaryAsync(
        string content,
        string? systemPrompt,
        int maxTokens = 150,
        double? temperature = null,
        string? operationName = null,
        string? promptVersion = null,
        string? fileName = null);
}
