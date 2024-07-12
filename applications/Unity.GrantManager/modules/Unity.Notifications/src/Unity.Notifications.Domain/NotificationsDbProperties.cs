namespace Unity.Notifications;

public static class NotificationsDbProperties
{
    public static string DbTablePrefix { get; set; } = "";

    public static string? DbSchema { get; set; } = "Notifications";

    public const string ConnectionStringName = "Tenant";
}
