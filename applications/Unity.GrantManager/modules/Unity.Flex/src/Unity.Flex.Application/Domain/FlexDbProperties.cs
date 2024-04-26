namespace Unity.Flex.Domain;

public static class FlexDbProperties
{
    public static string DbTablePrefix { get; set; } = "Flex";

    public static string? DbSchema { get; set; } = null;

    public const string ConnectionStringName = "Flex";
}
