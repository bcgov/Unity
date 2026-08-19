namespace Unity.Notifications.Logs;

public enum NotificationLogType
{
    SignalRSystemNotification = 0,
    SignalRDirectMessage = 1,
    SignalRGroupNotification = 2,
    DbException = 3,
    AbpHandledException = 4,
    MiddlewareUnhandledException = 5,
    PrometheusErrorCounterEvent = 6,
    PrometheusExceptionCounterEvent = 7,
    LegacyBridgeEvent = 8,
    UnityException = 9
}