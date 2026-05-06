using System.Threading.Tasks;

namespace Unity.AI.Web.PromptTools;

public interface IAIPromptToolAccessProvider
{
    Task<bool> CanViewPromptToolsAsync();
    string DefaultPromptVersion { get; }
}
