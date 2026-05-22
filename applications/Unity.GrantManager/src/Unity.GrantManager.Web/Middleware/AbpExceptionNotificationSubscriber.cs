using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Unity.GrantManager.Notifications;
using Unity.Notifications.TeamsNotifications;
using Volo.Abp.ExceptionHandling;

namespace Unity.GrantManager.Web.Middleware;

/// <summary>
/// Hooks into ABP's exception pipeline via IExceptionSubscriber.
/// ABP calls this for every exception it handles (controller actions, app services, etc.)
/// — complementing ExceptionCounterMiddleware which only catches exceptions that bypass ABP.
/// Registered explicitly in GrantManagerWebModule.ConfigureServices.
/// </summary>
public class AbpExceptionNotificationSubscriber(
    ExceptionNotificationThrottle throttle,
    IServiceScopeFactory scopeFactory,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AbpExceptionNotificationSubscriber> logger)
    : IExceptionSubscriber
{
    private static readonly HashSet<string> NotifyEnvironments =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Production",
            "Test",
            "Development"
        };

    public Task HandleAsync(ExceptionNotificationContext context)
    {
        Exception ex = context.Exception;

        logger.LogInformation(
            "[ExceptionNotify] Processing exception {ExceptionType}",
            ex.GetType().FullName);

        // Increment Prometheus counters
        ErrorCountingLoggerSink.ErrorCounter
            .WithLabels("error", ex.GetType().Name)
            .Inc();

        TryQueueTeamsNotification(ex);

        return Task.CompletedTask;
    }

    private void TryQueueTeamsNotification(Exception ex)
    {
        string? env =
            Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        if (string.IsNullOrWhiteSpace(env) ||
            !NotifyEnvironments.Contains(env))
        {
            return;
        }

        if (!throttle.ShouldNotify(ex.GetType().Name))
        {
            logger.LogDebug(
                "[ExceptionNotify] Notification throttled for {ExceptionType}",
                ex.GetType().Name);

            return;
        }

        // Fire-and-forget by design.
        // Notification failures are handled internally.
        _ = SendNotificationAsync(ex, env);
    }

    private async Task SendNotificationAsync(
        Exception ex,
        string environment)
    {
        try
        {
            await using AsyncServiceScope scope =
                scopeFactory.CreateAsyncScope();

            IServiceProvider services = scope.ServiceProvider;

            var notifications =
                services.GetRequiredService<INotificationsAppService>();

            string endpoint = GetEndpoint();

            var frame =
                ExceptionNotificationHelpers.GetTopFrame(ex);

            string sourceFile =
                ExceptionNotificationHelpers.NormalizeRepoPath(
                    frame?.File ?? "(unknown)");

            int? sourceLine = frame?.Line;

            string userName =
                AbpUserTenantAccessor.GetCurrentUserName(services)
                ?? "unknown";

            string tenantName =
                AbpUserTenantAccessor.GetCurrentTenantName(services)
                ?? "unknown";

            string activityTitle =
                $"[{environment.ToUpperInvariant()}] {ex.GetType().Name}";

            string activitySubtitle =
                $"Environment: {environment} | {endpoint} | {userName}@{tenantName}";

            List<Fact> facts = BuildFacts(
                ex,
                endpoint,
                userName,
                tenantName,
                sourceFile,
                sourceLine);

            await EnrichWithBlameInfoAsync(
                services,
                facts,
                sourceFile,
                sourceLine);

            await notifications.PostToTeamsAsync(
                activityTitle,
                activitySubtitle,
                facts);
        }
        catch (Exception notificationException)
        {
            logger.LogWarning(
                notificationException,
                "Failed to send Teams exception notification");
        }
    }

    private string GetEndpoint()
    {
        HttpContext? httpContext = httpContextAccessor.HttpContext;

        if (httpContext == null)
        {
            return "(background)";
        }

        return $"{httpContext.Request.Method} {httpContext.Request.Path}";
    }

    private static List<Fact> BuildFacts(
        Exception ex,
        string endpoint,
        string userName,
        string tenantName,
        string sourceFile,
        int? sourceLine)
    {
        string exceptionType =
            ex.GetType().FullName ?? ex.GetType().Name;

        string innerMessage =
            ex.InnerException?.Message ?? string.Empty;

        string stackTrace =
            ExceptionNotificationHelpers.BuildApplicationStackExcerpt(ex);

        return ExceptionNotificationHelpers.BuildFacts(
            exceptionType,
            ex.Message,
            endpoint,
            userName,
            tenantName,
            stackTrace,
            sourceFile,
            sourceLine,
            ExceptionCounterMiddleware.CommitSha,
            innerMessage);
    }

    private async Task EnrichWithBlameInfoAsync(
        IServiceProvider services,
        List<Fact> facts,
        string sourceFile,
        int? sourceLine)
    {
        if (!sourceLine.HasValue)
        {
            return;
        }

        try
        {
            string blamePath =
                ExceptionNotificationHelpers.BuildBlamePath(sourceFile);

            var blameService =
                services.GetRequiredService<IBlameLookupService>();

            var blame =
                await blameService.GetBlameAsync(
                    blamePath,
                    sourceLine.Value);

            if (blame == null)
            {
                return;
            }

            logger.LogInformation(
                "[ExceptionNotify] Blame lookup successful: {Author} {Commit}",
                blame.Author,
                blame.CommitSha);

            AddAuthorFact(facts, blame);

            AddCommitFact(facts, blame);

            AddPullRequestFacts(facts, blame);
        }
        catch (Exception blameException)
        {
            logger.LogWarning(
                blameException,
                "Failed to enrich exception with GitHub blame information");
        }
    }

    private static void AddAuthorFact(
        ICollection<Fact> facts,
        GitHubBlameInfo blame)
    {
        facts.Add(new Fact
        {
            Name = "Author",
            Value = $"{blame.Author} <{blame.Email}>"
        });
    }

    private static void AddCommitFact(
        ICollection<Fact> facts,
        GitHubBlameInfo blame)
    {
        string shortSha =
            GetShortSha(blame.CommitSha);

        facts.Add(new Fact
        {
            Name = "Commit",
            Value = $"{shortSha} {blame.Message}"
        });
    }

    private static void AddPullRequestFacts(
        ICollection<Fact> facts,
        GitHubBlameInfo blame)
    {
        if (string.IsNullOrWhiteSpace(blame.PullRequestUrl))
        {
            return;
        }

        facts.Add(new Fact
        {
            Name = "PR",
            Value =
                $"#{blame.PullRequestNumber} {blame.PullRequestUrl}"
        });

        if (string.IsNullOrWhiteSpace(blame.PullRequestTitle))
        {
            return;
        }

        facts.Add(new Fact
        {
            Name = "PR Title",
            Value = blame.PullRequestTitle
        });
    }

    private static string GetShortSha(string? sha)
    {
        if (string.IsNullOrWhiteSpace(sha))
        {
            return string.Empty;
        }

        return sha.Length > 7
            ? sha[..7]
            : sha;
    }
}