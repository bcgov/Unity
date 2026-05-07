using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Prometheus;
using Unity.GrantManager.Notifications;
using Unity.Notifications.TeamsNotifications;

namespace Unity.GrantManager.Web.Middleware;

public class ExceptionCounterMiddleware(RequestDelegate next, INotificationsAppService notificationsAppService)
{
    private static readonly Counter ExceptionCounter =
        Metrics.CreateCounter(
            "application_exceptions_total",
            "Total number of application exceptions",
            new CounterConfiguration
            {
                LabelNames = ["type"]
            });

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            ExceptionCounter.WithLabels(ex.GetType().Name).Inc();
            ErrorCountingLoggerSink.ErrorCounter.WithLabels("critical", ex.GetType().Name).Inc();
            await NotifyTeamsAsync(context, ex);
            throw;
        }
    }

    private async Task NotifyTeamsAsync(HttpContext context, Exception ex)
    {
        try
        {
            string? env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            string endpoint = $"{context.Request.Method} {context.Request.Path}";

            // Truncate stack trace — Teams message cards have a ~28 KB body limit
            string stackTrace = ex.StackTrace ?? "(no stack trace)";
            if (stackTrace.Length > 1500)
            {
                stackTrace = stackTrace[..1500] + "\n... (truncated)";
            }

            string activityTitle = $"[CRITICAL] {ex.GetType().Name}";
            string activitySubtitle = $"Environment: {env} | {endpoint}";

            var facts = new List<Fact>
            {
                new() { Name = "Exception", Value = ex.GetType().FullName ?? ex.GetType().Name },
                new() { Name = "Message",   Value = ex.Message },
                new() { Name = "Endpoint",  Value = endpoint },
                new() { Name = "Stack Trace", Value = stackTrace },
            };

            if (ex.InnerException is not null)
            {
                facts.Add(new Fact { Name = "Inner Exception", Value = ex.InnerException.Message });
            }

            await notificationsAppService.PostToTeamsAsync(activityTitle, activitySubtitle, facts);
        }
        catch
        {
            // Never let a Teams notification failure affect request handling
        }
    }
}
