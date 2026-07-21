using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Prometheus;
using Unity.GrantManager.Notifications;
using Volo.Abp.Uow;
using Unity.GrantManager.Notifications.Logs;
using Unity.GrantManager.Logs;

namespace Unity.GrantManager.Web.Middleware;

public class ExceptionCounterMiddleware(
    RequestDelegate next,
    ExceptionNotificationThrottle throttle,
    ILogger<ExceptionCounterMiddleware> logger)
{
    // Notify only in these environments; add "Staging" if desired
    private static readonly HashSet<string> NotifyEnvironments =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Production",
            "Test",
            "Development"
        };

    // Internal so AbpExceptionNotificationSubscriber can also increment it for ABP-handled
    // exceptions — the OpenShift "UnityHighExceptionRate" alert keys off this metric's "type"
    // label, so both handled and unhandled exceptions need to feed it.
    internal static readonly Counter ExceptionCounter =
        Metrics.CreateCounter(
            "application_exceptions_total",
            "Total number of application exceptions",
            new CounterConfiguration
            {
                LabelNames = new[] { "type" }
            });

    internal static readonly string CommitSha = ParseCommitSha(
        typeof(ExceptionCounterMiddleware).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion);

    internal static readonly string CommitAuthor =
        typeof(ExceptionCounterMiddleware).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(a => a.Key == "CommitAuthor")?.Value ?? "unknown";

    private static string ParseCommitSha(string? informationalVersion)
    {
        if (string.IsNullOrWhiteSpace(informationalVersion))
        {
            return "unknown";
        }

        var plusIndex = informationalVersion.IndexOf('+');

        return plusIndex >= 0
            ? informationalVersion[(plusIndex + 1)..]
            : informationalVersion;
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

            ErrorCountingLoggerSink.ErrorCounter
                .WithLabels("fatal", ex.GetType().Name)
                .Inc();

            QueueLogNotification(context, ex);

            throw;
        }
    }

    // Repo path and frame helpers are provided by ExceptionNotificationHelpers to avoid duplication

    private void QueueLogNotification(HttpContext context, Exception ex)
    {
        string? env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        string correlationId = context.TraceIdentifier;

        if (!NotifyEnvironments.Contains(env ?? string.Empty))
        {
            return;
        }

        if (!throttle.ShouldNotify(ex.GetType().Name))
        {
            return;
        }

        // Use the real root exception
        ex = ex.GetBaseException();

        // Capture values from the request context before it is disposed
        string endpoint = $"{context.Request.Method} {context.Request.Path}";
        string exTypeName = ex.GetType().FullName ?? ex.GetType().Name;
        string exMessage = ex.Message;
        string innerMessage = ex.InnerException?.Message ?? string.Empty;

        // Compact stack trace with only application frames
        string stackTrace = ExceptionNotificationHelpers.BuildApplicationStackExcerpt(ex);

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
                var exceptionLogs = scope.ServiceProvider.GetService<IExceptionLogAppService>();

                // Get current user and tenant name
                var userId = AbpUserTenantAccessor.GetCurrentUserId(scope.ServiceProvider);
                var userName = AbpUserTenantAccessor.GetCurrentUserName(scope.ServiceProvider) ?? "unknown";
                var tenantName = await AbpUserTenantAccessor.GetCurrentTenantNameAsync(scope.ServiceProvider) ?? "unknown";

                // Determine top frame (file/line) for initial facts so variables exist when creating the list
                var topForFacts = ExceptionNotificationHelpers.GetTopFrame(ex);
                string sourceFile = ExceptionNotificationHelpers.NormalizeRepoPath(topForFacts?.File ?? "(unknown)");
                int? sourceLine = topForFacts?.Line;

                var facts = ExceptionNotificationHelpers.BuildFacts(
                    exTypeName,
                    exMessage,
                    endpoint,
                    userName,
                    tenantName,
                    stackTrace,
                    sourceFile,
                    sourceLine,
                    CommitSha,
                    innerMessage);

                // Try to enrich with blame info similar to AbpExceptionNotificationSubscriber
                GitHubBlameInfo? blame = null;
                try
                {
                    if (sourceLine.HasValue)
                    {
                        // Blame enrichment is best-effort here: don't let failures block notifications
                        string blamePath = ExceptionNotificationHelpers.BuildBlamePath(sourceFile);
                        var blameService = scope.ServiceProvider.GetService<IBlameLookupService>();
                        if (blameService != null)
                        {
                            try
                            {
                                blame = await blameService.GetBlameAsync(blamePath, sourceLine.Value);
                                if (blame != null)
                                {
                                    facts.Add(new Fact { Name = "Author", Value = $"{blame.Author} <{blame.Email}>" });
                                    var shortSha = !string.IsNullOrEmpty(blame.CommitSha) && blame.CommitSha.Length > 7 ? blame.CommitSha.Substring(0, 7) : blame.CommitSha;
                                    facts.Add(new Fact { Name = "Commit", Value = $"{shortSha} {blame.Message}" });

                                    if (blame.PullRequestUrl != null)
                                    {
                                        facts.Add(new Fact { Name = "PR", Value = $"#{blame.PullRequestNumber} {blame.PullRequestUrl}" });
                                        if (!string.IsNullOrWhiteSpace(blame.PullRequestTitle))
                                        {
                                            facts.Add(new Fact { Name = "PR Title", Value = blame.PullRequestTitle });
                                        }
                                    }
                                }
                            }
                            catch (Exception blameEx)
                            {
                                logger.LogDebug(blameEx, "Blame lookup failed; continuing without blame info for {File}:{Line}", sourceFile, sourceLine);
                            }
                        }
                        else
                        {
                            logger.LogDebug("Blame lookup service not registered; skipping blame enrichment for {File}:{Line}", sourceFile, sourceLine);
                        }
                    }
                }
                catch (Exception ex2)
                {
                    // Catch-all: ensure notifications still send even if enrichment logic fails
                    logger.LogDebug(ex2, "Unexpected error during blame enrichment; continuing without blame info for {File}:{Line}", sourceFile, sourceLine);
                }

                // Provide simple activity title/subtitle for the notification
                var activityTitle = $"{exTypeName} thrown at {endpoint}";
                var activitySubtitle = $"Environment: {env} | {endpoint} | {userName}@{tenantName}";

                // Ensure a Unit-of-Work is active for any DB access inside NotificationsAppService
                try
                {
                    using var uow = uowManager.Begin(requiresNew: true, isTransactional: false);

                    // Try to post notification, but don't let failures prevent exception logging
                    try
                    {
                        await notifications.PostToNotificationsAsync(
                            activityTitle,
                            activitySubtitle,
                            facts);
                    }
                    catch (Exception notificationEx)
                    {
                        logger.LogWarning(notificationEx, "Failed to post Teams notification");
                    }

                    // Always attempt to log the exception, even if notification failed
                    if (exceptionLogs != null)
                    {
                        try
                        {
                            await exceptionLogs.CreateAsync(new CreateExceptionLogDto
                            {
                                UserId = userId,
                                UserName = userName,
                                TenantName = tenantName,
                                NotificationType = ExceptionLogType.MiddlewareUnhandledException,
                                Channel = ExceptionLogChannel.ExceptionPipeline,
                                Severity = ExceptionLogSeverity.Error,
                                Title = activityTitle,
                                Message = exMessage,
                                Source = nameof(ExceptionCounterMiddleware),
                                SourceReference = endpoint,
                                CorrelationId = correlationId,
                                IsDeliveredRealtime = false,
                                ExceptionType = exTypeName,
                                ExceptionMessage = exMessage,
                                StackExcerpt = stackTrace,
                                SourceFile = sourceFile,
                                SourceLine = sourceLine,
                                CommitSha = CommitSha,
                                Environment = env,
                                PayloadJson = null,
                                BlameAuthor = blame?.Author,
                                BlameEmail = blame?.Email,
                                BlameCommitSha = blame?.CommitSha,
                                BlameCommitMessage = blame?.Message,
                                PullRequestUrl = blame?.PullRequestUrl,
                                PullRequestNumber = blame?.PullRequestNumber,
                                PullRequestTitle = blame?.PullRequestTitle,
                                TicketReference = ExceptionNotificationHelpers.ExtractTicketReference(blame?.PullRequestTitle)
                            });
                        }
                        catch (Exception logEx)
                        {
                            logger.LogWarning(logEx, "Failed to create exception log within UnitOfWork");
                        }
                    }

                    await uow.CompleteAsync();
                }
                catch (Exception uowEx)
                {
                    logger.LogWarning(uowEx, "Failed to complete UnitOfWork for exception handling");
                }
            }
            catch (Exception notifyEx)
            {
                logger.LogWarning(
                    notifyEx,
                    "Failed to send Teams exception notification");
            }
        });
    }

    private static string BuildApplicationStackExcerpt(Exception ex)
    {
        return ExceptionNotificationHelpers.BuildApplicationStackExcerpt(ex);
    }
}
