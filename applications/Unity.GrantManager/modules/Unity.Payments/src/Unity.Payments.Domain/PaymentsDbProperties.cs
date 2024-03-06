namespace Unity.Payments;

public static class PaymentsDbProperties
{
    public static string DbTablePrefix { get; set; } = string.Empty;

    public static string? DbSchema { get; set; } = "Payments";

    public const string ConnectionStringName = "Payments";
}
