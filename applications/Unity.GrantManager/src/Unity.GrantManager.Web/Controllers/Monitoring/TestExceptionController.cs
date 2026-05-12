using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Unity.GrantManager.Notifications;
using Unity.Notifications.TeamsNotifications;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Uow;

namespace Unity.GrantManager.Web.Controllers.Monitoring;

/// <summary>
/// Temporary test endpoints — force exceptions/logs/notifications to verify
/// Prometheus error counting and Teams alerting. REMOVE BEFORE MERGING TO MAIN.
/// </summary>
[ApiController]
[Route("api/monitoring/test")]
[AllowAnonymous]
public class TestExceptionController : AbpControllerBase
{
    // Same SHA parsing as ExceptionCounterMiddleware
    private static readonly string CommitSha = ParseCommitSha(
        typeof(TestExceptionController).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion);

    private static string ParseCommitSha(string? informationalVersion)
    {
        if (string.IsNullOrWhiteSpace(informationalVersion)) return "unknown";
        var plusIndex = informationalVersion.IndexOf('+');
        return plusIndex >= 0 ? informationalVersion[(plusIndex + 1)..] : informationalVersion;
    }

    /// <summary>
    /// GET /api/monitoring/test/log-error
    /// Logs an Error-level Serilog event → increments application_errors_total via ErrorCountingLoggerSink.
    /// </summary>
    [HttpGet("log-error")]
    public IActionResult LogError()
    {
        var ex = new InvalidOperationException("Test exception for Prometheus error counter verification.");
        Logger.LogError(ex, "Test error log for application_errors_total counter — CommitSha: {CommitSha}", CommitSha);
        return Ok(new { logged = true, commitSha = CommitSha, message = "Error logged — check /metrics for application_errors_total." });
    }

    /// <summary>
    /// GET /api/monitoring/test/notify
    /// Directly fires the Teams notification with commit SHA — same path as ExceptionCounterMiddleware.
    /// ABP catches controller exceptions before they reach the middleware, so this endpoint
    /// exercises the notification code directly.
    /// </summary>
    [HttpGet("notify")]
    public async Task<IActionResult> NotifyTeams()
    {
        var ex = new InvalidOperationException("Test exception for Teams notification verification.");
        string env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown";
        string endpoint = $"{Request.Method} {Request.Path}";

        var scopeFactory = HttpContext.RequestServices.GetRequiredService<IServiceScopeFactory>();

        await using var scope = scopeFactory.CreateAsyncScope();
        var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationsAppService>();

        using var uow = uowManager.Begin(requiresNew: true, isTransactional: false);

        var facts = new List<Fact>
        {
            new() { Name = "Exception",   Value = ex.GetType().FullName ?? ex.GetType().Name },
            new() { Name = "Message",     Value = ex.Message },
            new() { Name = "Endpoint",    Value = endpoint },
            new() { Name = "Stack Trace", Value = ex.StackTrace ?? "(no stack trace)" },
            new() { Name = "Commit",      Value = CommitSha },
        };

        await notifications.PostToTeamsAsync(
            $"[TEST] {ex.GetType().Name}",
            $"Environment: {env} | {endpoint}",
            facts);

        await uow.CompleteAsync();

        return Ok(new { notified = true, commitSha = CommitSha, environment = env });
    }
}
