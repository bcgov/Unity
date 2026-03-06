namespace Unity.AI.Domain;

public static class AIDbProperties
{
    public static string DbTablePrefix { get; set; } = string.Empty;

    /// <summary>
    /// Schema for Unity.AI tables — kept separate from other modules.
    /// </summary>
    public static string? DbSchema { get; set; } = "AI";

    /// <summary>
    /// Shares the Tenant connection string so no additional database infrastructure is required.
    /// </summary>
    public const string ConnectionStringName = "Tenant";
}
