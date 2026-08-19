using Prometheus;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Unity.GrantManager.Logs;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Users;
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
    private static readonly TimeSpan PersistenceBackoff = TimeSpan.FromSeconds(30);
    private static IServiceScopeFactory? _scopeFactory;
    private static readonly AsyncLocal<bool> IsPersistingExceptionLog = new();
    private readonly object _persistenceGate = new();
    private bool _persistenceInFlight;
    private DateTimeOffset _persistenceDisabledUntil;

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

        Guid? tenantId = null;
        Guid? userId = null;
        string? userName = null;

        try
        {
            using var metadataScope = scopeFactory.CreateScope();
            var currentTenant = metadataScope.ServiceProvider.GetRequiredService<ICurrentTenant>();
            var currentUser = metadataScope.ServiceProvider.GetRequiredService<ICurrentUser>();
            tenantId = currentTenant.Id;
            userId = currentUser.Id;
            userName = AbpUserTenantAccessor.GetCurrentUserName(metadataScope.ServiceProvider);
        }
        catch
        {
            // Persistence is best-effort; continue with host/unknown metadata.
        }

        lock (_persistenceGate)
        {
            if (_persistenceInFlight || DateTimeOffset.UtcNow < _persistenceDisabledUntil)
            {
                return;
            }

            _persistenceInFlight = true;
        }

        // Do not inherit the request's ambient ABP unit of work. The request may be
        // disposing its DbContext while this fire-and-forget persistence is running.
        using (ExecutionContext.SuppressFlow())
        {
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

                    using (scope.ServiceProvider.GetRequiredService<ICurrentTenant>().Change(tenantId))
                    {
                        var frame = logEvent.Exception == null
                            ? null
                            : ExceptionNotificationHelpers.GetTopFrame(logEvent.Exception);
                        string? sourceFile = frame?.File == null
                            ? null
                            : ExceptionNotificationHelpers.NormalizeRepoPath(frame.Value.File);

                        // A fresh unit of work owns the context used by the background write.
                        using var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>()
                            .Begin(requiresNew: true, isTransactional: false);

                        await exceptionLogs.CreateAsync(new CreateExceptionLogDto
                        {
                            UserId = userId,
                            UserName = userName,
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
                }
                catch
                {
                    lock (_persistenceGate)
                    {
                        _persistenceDisabledUntil = DateTimeOffset.UtcNow.Add(PersistenceBackoff);
                    }
                }
                finally
                {
                    IsPersistingExceptionLog.Value = false;

                    lock (_persistenceGate)
                    {
                        _persistenceInFlight = false;
                    }
                }
            });
        }
    }
}
