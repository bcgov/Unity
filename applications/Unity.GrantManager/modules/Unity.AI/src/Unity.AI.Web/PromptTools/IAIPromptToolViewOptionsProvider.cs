namespace Unity.GrantManager.Web.AI
{
    public interface IAIPromptToolViewOptionsProvider
    {
        bool IsDevPromptControlsEnabled { get; }

        string DefaultPromptVersion { get; }
    }
}
