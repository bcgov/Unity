namespace Unity.AI.Settings;

public static class AISettings
{
    public const string AutomaticGenerationEnabled = "GrantManager.AI.AutomaticGenerationEnabled";
    public const string ManualGenerationEnabled    = "GrantManager.AI.ManualGenerationEnabled";
    public const string ReportingEnabled           = "GrantManager.AI.ReportingEnabled";

    /// <summary>Comma-separated schema types AI Reporting embeds for the tenant (sent to UAI in the token).</summary>
    public const string ReportingSchemaTypes       = "GrantManager.AI.ReportingSchemaTypes";

    public const string DefaultReportingSchemaTypes = "public,custom";
}
