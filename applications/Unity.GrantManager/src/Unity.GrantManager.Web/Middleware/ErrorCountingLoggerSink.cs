using Prometheus;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Unity.GrantManager.Logs;
using Volo.Abp.Uow;

namespace Unity.GrantManager.Web.Middleware;

/// <summary>
/// Shared Prometheus counter for application-level errors.
/// Labelled by log level ("error" / "fatal") and exception type (empty when no exception).
/// Implemented as a Serilog ILogEventSink so it works alongside UseSerilog().
/// Register via: .WriteTo.Sink(new ErrorCountingLoggerSink())
/// </summary>
public sealed class ErrorCountingLoggerSink : ILogEventSink
{
    private static IServiceScopeFactory? _scopeFactory;
    private static readonly AsyncLocal<bool> IsPersistingExceptionLog = new();

    internal static readonly Counter ErrorCounter =
        Metrics.CreateCounter(
            "application_errors_total",
            "Total application errors captured via Serilog",
            new CounterConfiguration
            {
                LabelNames = ["level", "exception"]
            });

    public static void SetScopeFactory(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void Emit(LogEvent logEvent)
    {
        if (logEvent.Level < LogEventLevel.Error || IsPersistingExceptionLog.Value)
        {
            return;
        }

        if (logEvent.Exception is not null &&
            ExceptionNotificationHelpers.IsExpected(logEvent.Exception))
        {
            return;
        }

        string level = logEvent.Level.ToString().ToLowerInvariant();
        string exceptionType = logEvent.Exception?.GetType().Name ?? string.Empty;
        ErrorCounter.WithLabels(level, exceptionType).Inc();

        var scopeFactory = _scopeFactory;

        if (scopeFactory == null)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            IsPersistingExceptionLog.Value = true;

            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var exceptionLogs = scope.ServiceProvider.GetService<IExceptionLogAppService>();

                if (exceptionLogs == null)
                {
                    return;
                }

                var frame = logEvent.Exception == null
                    ? null
                    : ExceptionNotificationHelpers.GetTopFrame(logEvent.Exception);
                string? sourceFile = frame?.File == null
                    ? null
                    : ExceptionNotificationHelpers.NormalizeRepoPath(frame.Value.File);

                // Isolate this from whatever ambient unit of work/DbContext happens to be flowing
                // through the captured ExecutionContext (e.g. the request that triggered this log
                // event may still be mid-operation on its own DbContext) - without requiresNew,
                // ABP's implicit [UnitOfWork] on CreateAsync would join that ambient one instead of
                // getting its own, causing "a second operation was started on this context instance".
                using var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>()
                    .Begin(requiresNew: true, isTransactional: false);

                await exceptionLogs.CreateAsync(new CreateExceptionLogDto
                {
                    UserId = AbpUserTenantAccessor.GetCurrentUserId(scope.ServiceProvider),
                    UserName = AbpUserTenantAccessor.GetCurrentUserName(scope.ServiceProvider),
                    TenantName = await AbpUserTenantAccessor.GetCurrentTenantNameAsync(scope.ServiceProvider),
                    NotificationType = logEvent.Exception == null
                        ? ExceptionLogType.PrometheusErrorCounterEvent
                        : ExceptionLogType.PrometheusExceptionCounterEvent,
                    Channel = ExceptionLogChannel.Prometheus,
                    Severity = logEvent.Level >= LogEventLevel.Fatal
                        ? ExceptionLogSeverity.Critical
                        : ExceptionLogSeverity.Error,
                    Title = "Prometheus Error Counter Event",
                    Message = logEvent.RenderMessage(),
                    Source = nameof(ErrorCountingLoggerSink),
                    IsDeliveredRealtime = false,
                    ExceptionType = logEvent.Exception?.GetType().FullName,
                    ExceptionMessage = logEvent.Exception?.Message,
                    StackExcerpt = logEvent.Exception?.StackTrace,
                    SourceFile = sourceFile,
                    SourceLine = frame?.Line,
                    Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                });

                await uow.CompleteAsync();
            }
            catch
            {
                // Swallow to avoid recursive logging from logger sink failures.
            }
            finally
            {
                IsPersistingExceptionLog.Value = false;
            }
        });
    }
}
