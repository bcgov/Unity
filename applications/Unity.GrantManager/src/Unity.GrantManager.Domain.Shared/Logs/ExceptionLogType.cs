using System.Text.Json.Serialization;

namespace Unity.GrantManager.Logs;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ExceptionLogType
{
    AbpHandledException = 0,
    MiddlewareUnhandledException = 1,
    PrometheusErrorCounterEvent = 2,
    PrometheusExceptionCounterEvent = 3,
    BackgroundJobException = 4,
    DbException = 5,
    UnhandledException = 6
}
