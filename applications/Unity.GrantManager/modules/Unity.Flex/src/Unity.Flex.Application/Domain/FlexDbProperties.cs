namespace Unity.Flex.Domain;

public static class FlexDbProperties
{
    public static string DbTablePrefix { get; set; } = string.Empty;
    public static string? DbSchema { get; set; } = "Flex";

    public const string ConnectionStringName = "Tenant";

    /* We leave this the same as the tenant db as no need to split this yet, 
     * we could use another connection string altogether if we split databases */
}
