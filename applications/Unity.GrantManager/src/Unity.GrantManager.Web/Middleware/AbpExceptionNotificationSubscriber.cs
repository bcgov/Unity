using System;
using System.Collections.Generic;
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
        logger.LogInformation("[ExceptionNotify] HandleAsync called for exception: {ExceptionType} - {Message}", context.Exception.GetType().FullName, context.Exception.Message);
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
        logger.LogInformation("[ExceptionNotify] QueueTeamsNotification called for exception: {ExceptionType} - {Message}", ex.GetType().FullName, ex.Message);
        string? env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        logger.LogInformation("[ExceptionNotify] Environment: {Env}", env);

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
        string stackTrace = ExceptionNotificationHelpers.BuildApplicationStackExcerpt(ex);

        _ = Task.Run(async () =>
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var uowManager = scope.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
                var notifications = scope.ServiceProvider.GetRequiredService<INotificationsAppService>();

                using var uow = uowManager.Begin(requiresNew: true, isTransactional: false);

                string activityTitle = $"[{env?.ToUpperInvariant()}] {ex.GetType().Name}";

                var frame = ExceptionNotificationHelpers.GetTopFrame(ex);
                string sourceFile = ExceptionNotificationHelpers.NormalizeRepoPath(frame?.File ?? "(unknown)");
                int? sourceLine = frame?.Line;

                var userName = AbpUserTenantAccessor.GetCurrentUserName(scope.ServiceProvider) ?? "unknown";
                var tenantName = AbpUserTenantAccessor.GetCurrentTenantName(scope.ServiceProvider) ?? "unknown";

                string activitySubtitle = $"Environment: {env} | {endpoint} | {userName}@{tenantName}";

                var facts = ExceptionNotificationHelpers.BuildFacts(
                    exTypeName,
                    exMessage,
                    endpoint,
                    userName,
                    tenantName,
                    stackTrace,
                    sourceFile,
                    sourceLine,
                    ExceptionCounterMiddleware.CommitSha,
                    innerMessage);

                if (sourceLine.HasValue)
                {
                    // Use required service to fail-fast if the blame lookup service is not registered
                    string blamePath = ExceptionNotificationHelpers.BuildBlamePath(sourceFile);
                    var blameService = scope.ServiceProvider.GetRequiredService<IBlameLookupService>();
                    var blame = await blameService.GetBlameAsync(blamePath, sourceLine.Value);

                    if (blame != null)
                    {
                        logger.LogInformation("[ExceptionNotify] Blame lookup result: Author={Author}, Commit={Commit}, PR={PR}, PRTitle={PRTitle}", blame.Author, blame.CommitSha, blame.PullRequestUrl, blame.PullRequestTitle);
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

                await notifications.PostToTeamsAsync(activityTitle, activitySubtitle, facts);
                await uow.CompleteAsync();
            }
            catch (Exception notifyEx)
            {
                logger.LogWarning(notifyEx, "Failed to send Teams exception notification via IExceptionSubscriber");
            }
        });
    }


}
