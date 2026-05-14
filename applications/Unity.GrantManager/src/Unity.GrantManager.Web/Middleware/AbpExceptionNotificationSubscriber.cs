using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Unity.GrantManager.Notifications;
using Unity.Notifications.TeamsNotifications;
using Volo.Abp.ExceptionHandling;
using Volo.Abp.Uow;

namespace Unity.GrantManager.Web.Middleware;

/// <summary>
/// Hooks into ABP's exception pipeline via IExceptionSubscriber.
/// ABP calls this for every exception it handles (controller actions, app services, etc.)
/// — complementing ExceptionCounterMiddleware which only catches exceptions that bypass ABP.
/// Registered explicitly in GrantManagerWebModule.ConfigurePolicies.
/// </summary>
public class AbpExceptionNotificationSubscriber(
    ExceptionNotificationThrottle throttle,
    IServiceScopeFactory scopeFactory,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AbpExceptionNotificationSubscriber> logger) : IExceptionSubscriber
{
    private static readonly HashSet<string> NotifyEnvironments =
        new(StringComparer.OrdinalIgnoreCase) { "Production", "Test", "Development" };

    public Task HandleAsync(ExceptionNotificationContext context)
    {
        var ex = context.Exception;

        // Increment Prometheus counters
        ErrorCountingLoggerSink.ErrorCounter
            .WithLabels("error", ex.GetType().Name)
            .Inc();

        QueueTeamsNotification(ex);

        return Task.CompletedTask;
    }

    private void QueueTeamsNotification(Exception ex)
    {
        string? env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        if (!NotifyEnvironments.Contains(env ?? string.Empty))
            return;

        if (!throttle.ShouldNotify(ex.GetType().Name))
            return;

        var httpContext = httpContextAccessor.HttpContext;
        string endpoint = httpContext != null
            ? $"{httpContext.Request.Method} {httpContext.Request.Path}"
            : "(background)";

        string exTypeName = ex.GetType().FullName ?? ex.GetType().Name;
        string exMessage = ex.Message;
        string innerMessage = ex.InnerException?.Message ?? string.Empty;
        string stackTrace = ex.StackTrace ?? "(no stack trace)";
        if (stackTrace.Length > 1500)
            stackTrace = stackTrace[..1500] + "\n... (truncated)";

        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                var notifications = scope.ServiceProvider.GetRequiredService<INotificationsAppService>();

                using var uow = uowManager.Begin(requiresNew: true, isTransactional: false);

                string activityTitle = $"[{env?.ToUpperInvariant()}] {ex.GetType().Name}";
                string activitySubtitle = $"Environment: {env} | {endpoint}";

                var frame = GetTopFrame(ex);
                string sourceFile = NormalizeRepoPath(frame?.File ?? "(unknown)");
                int? sourceLine = frame?.Line;

                var facts = new List<Fact>
                {
                    new() { Name = "Exception",   Value = exTypeName },
                    new() { Name = "Message",     Value = exMessage },
                    new() { Name = "Endpoint",    Value = endpoint },
                    new() { Name = "Stack Trace", Value = stackTrace },
                    new() { Name = "Source",      Value = sourceLine.HasValue ? $"{sourceFile}:{sourceLine}" : sourceFile },
                    new() { Name = "Commit",      Value = ExceptionCounterMiddleware.CommitSha },
                    new() { Name = "Author",      Value = ExceptionCounterMiddleware.CommitAuthor },
                };

                if (!string.IsNullOrEmpty(innerMessage))
                    facts.Add(new Fact { Name = "Inner Exception", Value = innerMessage });

                if (sourceLine.HasValue)
                {
                    try
                    {
                        var blameService = scope.ServiceProvider.GetRequiredService<IBlameLookupService>();
                        var blame = await blameService.GetBlameAsync(sourceFile, sourceLine.Value);
                        
                        if (blame != null)
                        {
                            logger.LogInformation("[ExceptionNotify] Blame lookup result: Author={Author}, Commit={Commit}, PR={PR}, PRTitle={PRTitle}", blame.Author, blame.CommitSha, blame.PullRequestUrl, blame.PullRequestTitle);
                            facts.Add(new Fact { Name = "Blame Author", Value = $"{blame.Author} <{blame.Email}>" });
                            var shortSha = !string.IsNullOrEmpty(blame.CommitSha) && blame.CommitSha.Length > 7 ? blame.CommitSha.Substring(0, 7) : blame.CommitSha;
                            facts.Add(new Fact { Name = "Blame Commit", Value = $"{shortSha} {blame.Message}" });

                            if (blame.PullRequestUrl != null)
                            {
                                facts.Add(new Fact { Name = "Blame PR", Value = $"#{blame.PullRequestNumber} {blame.PullRequestUrl}" });
                                if (!string.IsNullOrWhiteSpace(blame.PullRequestTitle))
                                {
                                    facts.Add(new Fact { Name = "Blame PR Title", Value = blame.PullRequestTitle });
                                }
                            }
                        }
                    }
                    catch (Exception blameEx)
                    {
                        logger.LogDebug(blameEx, "Blame lookup failed for {File}:{Line}", sourceFile, sourceLine);
                    }
                }

                await notifications.PostToTeamsAsync(activityTitle, activitySubtitle, facts);
                await uow.CompleteAsync();
            }
            catch (Exception notifyEx)
            {
                logger.LogWarning(notifyEx, "Failed to send Teams exception notification via IExceptionSubscriber");
            }
        });
    }

    private static string NormalizeRepoPath(string fullPath)
    {
        const string marker = "src/";

        int idx = fullPath.IndexOf(marker, StringComparison.OrdinalIgnoreCase);

        if (idx < 0)
            return fullPath.Replace("\\", "/");

        return fullPath[(idx + marker.Length)..]
            .Replace("\\", "/");
    }

    private static (string? File, int? Line)? GetTopFrame(Exception ex)
    {
        var trace = new StackTrace(ex, true);

        foreach (var frame in trace.GetFrames() ?? [])
        {
            var file = frame.GetFileName();
            var line = frame.GetFileLineNumber();

            if (!string.IsNullOrWhiteSpace(file) && line > 0)
            {
                return (file, line);
            }
        }

        return null;
    }
}
