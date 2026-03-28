namespace Unity.AI.Web.PromptTools;

public interface IAIPromptToolViewOptionsProvider
{
    bool IsDevPromptControlsEnabled { get; }
    string DefaultPromptVersion { get; }
}
