namespace Unity.Reporting;

public static class ReportingDbProperties
{
    public static string DbTablePrefix { get; set; } = "Reporting";

    public static string? DbSchema { get; set; } = null;

    public const string ConnectionStringName = "Reporting";
}
