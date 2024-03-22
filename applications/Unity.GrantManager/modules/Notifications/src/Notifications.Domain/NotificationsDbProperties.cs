namespace Notifications;

public static class NotificationsDbProperties
{
    public static string DbTablePrefix { get; set; } = "Notifications";

    public static string? DbSchema { get; set; } = null;

    public const string ConnectionStringName = "Notifications";
}
