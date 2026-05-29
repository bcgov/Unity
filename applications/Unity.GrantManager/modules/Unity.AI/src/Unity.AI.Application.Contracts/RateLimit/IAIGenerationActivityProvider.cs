using System.Threading.Tasks;

namespace Unity.AI.RateLimit;

public interface IAIGenerationActivityProvider
{
    Task<bool> HasActiveGenerationAsync();
}
