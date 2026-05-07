using System;
using Microsoft.Extensions.Logging;
using Prometheus;
using Serilog.Core;
using Serilog.Events;

namespace Unity.GrantManager.Web.Middleware;

/// <summary>
/// Shared Prometheus counter for application-level errors.
/// Labelled by log level ("error" / "critical") and exception type (empty when no exception).
/// Implemented as a Serilog ILogEventSink so it works alongside UseSerilog().
/// Register via: .WriteTo.Sink(new ErrorCountingLoggerSink())
/// </summary>
public sealed class ErrorCountingLoggerSink : ILogEventSink
{
    internal static readonly Counter ErrorCounter =
        Metrics.CreateCounter(
            "application_errors_total",
            "Total application errors captured via Serilog",
            new CounterConfiguration
            {
                LabelNames = ["level", "exception"]
            });

    public void Emit(LogEvent logEvent)
    {
        if (logEvent.Level < LogEventLevel.Error) return;

        string level = logEvent.Level.ToString().ToLowerInvariant();
        string exceptionType = logEvent.Exception?.GetType().Name ?? string.Empty;
        ErrorCounter.WithLabels(level, exceptionType).Inc();
    }
}
