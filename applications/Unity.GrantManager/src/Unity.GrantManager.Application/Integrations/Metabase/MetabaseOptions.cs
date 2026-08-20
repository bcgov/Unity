namespace Unity.GrantManager.Integrations.Metabase;

public class MetabaseOptions
{
    /// <summary>Admin API key - same key the Metabase admin UI/PowerShell runbook uses (x-api-key header).</summary>
    public string ApiKey { get; set; } = string.Empty;
}
