using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prometheus;
using Unity.GrantManager.Notifications;
using Unity.Notifications.TeamsNotifications;
using Volo.Abp.Uow;

namespace Unity.GrantManager.Web.Middleware;

public class ExceptionCounterMiddleware(
    RequestDelegate next,
    ExceptionNotificationThrottle throttle,
    ILogger<ExceptionCounterMiddleware> logger)
{
    // Notify only in these environments; add "Staging" if desired
    private static readonly HashSet<string> NotifyEnvironments =
        new(StringComparer.OrdinalIgnoreCase) { "Production", "Test", "Development" };

    private static readonly Counter ExceptionCounter =
        Metrics.CreateCounter(
            "application_exceptions_total",
            "Total number of application exceptions",
            new CounterConfiguration
            {
                LabelNames = ["type"]
            });

    // Git SHA baked in at build time via -p:SourceRevisionId=<sha> in the Dockerfile.
    // Format is "<version>+<sha>" e.g. "1.0.0+a3f8c21"; we extract just the SHA.
    private static readonly string CommitSha = ParseCommitSha(
        typeof(ExceptionCounterMiddleware).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion);

    private static string ParseCommitSha(string? informationalVersion)
    {
        if (string.IsNullOrWhiteSpace(informationalVersion)) return "unknown";
        var plusIndex = informationalVersion.IndexOf('+');
        return plusIndex >= 0 ? informationalVersion[(plusIndex + 1)..] : informationalVersion;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            ExceptionCounter.WithLabels(ex.GetType().Name).Inc();
            ErrorCountingLoggerSink.ErrorCounter.WithLabels("fatal", ex.GetType().Name).Inc();

            QueueTeamsNotification(context, ex);

            throw;
        }
    }

    private void QueueTeamsNotification(HttpContext context, Exception ex)
    {
        string? env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        if (!NotifyEnvironments.Contains(env ?? string.Empty))
        {
            return;
        }

        if (!throttle.ShouldNotify(ex.GetType().Name))
        {
            return;
        }

        // Capture values from the request context before it is disposed
        string endpoint = $"{context.Request.Method} {context.Request.Path}";
        string exTypeName = ex.GetType().FullName ?? ex.GetType().Name;
        string exMessage = ex.Message;
        string innerMessage = ex.InnerException?.Message ?? string.Empty;
        string stackTrace = ex.StackTrace ?? "(no stack trace)";
        if (stackTrace.Length > 1500)
        {
            stackTrace = stackTrace[..1500] + "\n... (truncated)";
        }

        // Resolve a scoped INotificationsAppService from a fresh DI scope so
        // we can safely use it after the request scope has ended
        var scopeFactory = context.RequestServices.GetRequiredService<IServiceScopeFactory>();

        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                var notifications = scope.ServiceProvider.GetRequiredService<INotificationsAppService>();

                using var uow = uowManager.Begin(requiresNew: true, isTransactional: false);

                string activityTitle = $"[CRITICAL] {ex.GetType().Name}";
                string activitySubtitle = $"Environment: {env} | {endpoint}";

                var facts = new List<Fact>
                {
                    new() { Name = "Exception",    Value = exTypeName },
                    new() { Name = "Message",      Value = exMessage },
                    new() { Name = "Endpoint",     Value = endpoint },
                    new() { Name = "Stack Trace",  Value = stackTrace },
                    new() { Name = "Commit",       Value = CommitSha },
                };

                if (!string.IsNullOrEmpty(innerMessage))
                {
                    facts.Add(new Fact { Name = "Inner Exception", Value = innerMessage });
                }

                await notifications.PostToTeamsAsync(activityTitle, activitySubtitle, facts);
                await uow.CompleteAsync();
            }
            catch (Exception notifyEx)
            {
                logger.LogWarning(notifyEx, "Failed to send Teams exception notification");
            }
        });
    }
}

