namespace Unity.Payments.Domain;

public static class PaymentsDbProperties
{
    public static string DbTablePrefix { get; set; } = string.Empty;

    public static string? DbSchema { get; set; } = "Payments";

    public const string ConnectionStringName = "Tenant"; 
    /* We leave this the same as the tenant db as no need to split this yet, we could use another connection string altogether if we split databases */
}
