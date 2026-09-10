using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Unity.Modules.Shared.Constants;
using Unity.Modules.Shared.Permissions;
using Unity.Notifications.EmailNotifications;

using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;

namespace Unity.GrantManager.Logs;

public class ExceptionLogAppService(
    IRepository<ExceptionLog, Guid> exceptionLogRepository,
    IDataFilter dataFilter,
    IEmailNotificationService emailNotificationService)
    : ApplicationService, IExceptionLogAppService
{
    private const string AlertFromAddress = "NoReply@gov.bc.ca";
    private const int AlertEmailSuppressionWindowDays = 5;

    // Called from background jobs and exception middleware where there may be no
    // current tenant/user, so this intentionally does not check CurrentTenant.
    [RemoteService(false)]
    public virtual async Task<Guid> CreateAsync(CreateExceptionLogDto input)
    {
        // Exceptions can originate from host-side/background contexts with no current tenant,
        // so the duplicate lookup must not be restricted to the caller's ambient tenant.
        using (dataFilter.Disable<IMultiTenant>())
        {
            var existing = await FindTodaysDuplicateAsync(input);
            if (existing != null)
            {
                ApplyOccurrence(existing, input);
                existing.OccurrenceCount++;
                await exceptionLogRepository.UpdateAsync(existing, autoSave: true);
                return existing.Id;
            }

            // Check before inserting today's row, otherwise the row we're about to create
            // would satisfy its own "has this happened recently" check.
            var seenRecently = await HasOccurredWithinAsync(input, AlertEmailSuppressionWindowDays);

            var exceptionLog = new ExceptionLog
            {
                TenantId = CurrentTenant.Id
            };
            ApplyOccurrence(exceptionLog, input);

            exceptionLog = await exceptionLogRepository.InsertAsync(exceptionLog, autoSave: true);

            // Only email when this error hasn't been seen in the last N days — a recurring
            // error still gets a fresh per-day row (for OccurrenceCount) but stays quiet by email.
            if (!seenRecently)
            {
                await TrySendAlertEmailAsync(exceptionLog);
            }

            return exceptionLog.Id;
        }
    }

    private async Task<bool> HasOccurredWithinAsync(CreateExceptionLogDto input, int days)
    {
        var since = DateTime.UtcNow.AddDays(-days);

        var queryable = await exceptionLogRepository.GetQueryableAsync();

        return await AsyncExecuter.AnyAsync(
            queryable
                .Where(x => x.CreationTime >= since)
                .Where(x => x.Source == input.Source)
                .Where(x => x.Environment == input.Environment)
                .Where(x => x.ExceptionType == input.ExceptionType)
                .Where(x => x.ExceptionMessage == input.ExceptionMessage));
    }

    private async Task TrySendAlertEmailAsync(ExceptionLog log)
    {
        try
        {
            var subject = $"Unity Exception Alert: {log.ExceptionType ?? log.Title}";
            var htmlBody = BuildAlertEmailBody(log);

            await emailNotificationService.SendEmailNotification(
                new EmailMessageParams(
                    UnityAlertConstants.UnityAlertEmail,
                    htmlBody,
                    subject,
                    AlertFromAddress,
                    ""),
                "html");
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Failed to send exception alert email for {ExceptionType}", log.ExceptionType);
        }
    }

    private static string BuildAlertEmailBody(ExceptionLog log)
    {
        var rows = new StringBuilder();

        AppendRow(rows, "Exception", log.ExceptionType);
        AppendRow(rows, "Message", log.ExceptionMessage ?? log.Message);
        AppendRow(rows, "Severity", log.Severity.ToString());
        AppendRow(rows, "Environment", log.Environment);
        AppendRow(rows, "Source", log.Source);
        AppendRow(rows, "Endpoint", log.SourceReference);
        AppendRow(rows, "Author", log.BlameAuthor);
        AppendRow(rows, "Source Line", log.SourceFile == null ? null : $"{log.SourceFile}:{log.SourceLine}");
        AppendRow(rows, "Line of Code", GetTopStackLine(log.StackExcerpt));
        AppendRow(rows, "User", log.UserName);
        AppendRow(rows, "Tenant", log.TenantName);
        AppendRow(rows, "Ticket", log.TicketReference);
        AppendRow(rows, "Correlation Id", log.CorrelationId);

        return $@"<html>
<body style='font-family: Arial, sans-serif;'>
    <h3 style='color: #a80000;'>{WebUtility.HtmlEncode(log.Title)}</h3>
    <table style='border-collapse: collapse;'>
{rows}
    </table>
    <p style='font-size: 12px; color: #999;'>*Note - Please do not reply to this email as it is an automated notification.</p>
</body>
</html>";
    }

    // Every row is a single short label/value line — keeps the alert scannable at a glance.
    private static void AppendRow(StringBuilder rows, string label, string? value, int maxLength = 200)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var trimmed = value.Length > maxLength ? value[..maxLength] + "…" : value;

        rows.Append("        <tr><td style='padding:4px 8px;font-weight:bold;border:1px solid #ddd;white-space:nowrap;vertical-align:top;'>")
            .Append(WebUtility.HtmlEncode(label))
            .Append("</td><td style='padding:4px 8px;border:1px solid #ddd;'>")
            .Append(WebUtility.HtmlEncode(trimmed))
            .Append("</td></tr>")
            .AppendLine();
    }

    // The first application frame in the stack excerpt reads as "ClassName.MethodName() in file:line" —
    // the closest thing to "the line of code" responsible, short of fetching the file from source control.
    private static string? GetTopStackLine(string? stackExcerpt)
    {
        if (string.IsNullOrWhiteSpace(stackExcerpt))
        {
            return null;
        }

        return stackExcerpt
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()
            ?.Trim();
    }

    // Refreshes every field captured for the current occurrence, so a recurring same-day error
    // always reflects the latest user/tenant, blame lookup, and source location — even if an
    // earlier occurrence of the same error was logged before that information was available.
    private static void ApplyOccurrence(ExceptionLog exceptionLog, CreateExceptionLogDto input)
    {
        exceptionLog.UserId = input.UserId;
        exceptionLog.UserName = input.UserName;
        exceptionLog.TenantName = input.TenantName;
        exceptionLog.NotificationType = input.NotificationType;
        exceptionLog.Channel = input.Channel;
        exceptionLog.Severity = input.Severity;
        exceptionLog.Title = input.Title;
        exceptionLog.Message = input.Message;
        exceptionLog.Source = input.Source;
        exceptionLog.SourceReference = input.SourceReference;
        exceptionLog.PayloadJson = input.PayloadJson;
        exceptionLog.CorrelationId = input.CorrelationId;
        exceptionLog.IsDeliveredRealtime = input.IsDeliveredRealtime;
        exceptionLog.DeliveryTarget = input.DeliveryTarget;
        exceptionLog.ExceptionType = input.ExceptionType;
        exceptionLog.ExceptionMessage = input.ExceptionMessage;
        exceptionLog.StackExcerpt = input.StackExcerpt;
        exceptionLog.SourceFile = input.SourceFile;
        exceptionLog.SourceLine = input.SourceLine;
        exceptionLog.CommitSha = input.CommitSha;
        exceptionLog.Environment = input.Environment;
        exceptionLog.BlameAuthor = input.BlameAuthor;
        exceptionLog.BlameEmail = input.BlameEmail;
        exceptionLog.BlameCommitSha = input.BlameCommitSha;
        exceptionLog.BlameCommitMessage = input.BlameCommitMessage;
        exceptionLog.PullRequestUrl = input.PullRequestUrl;
        exceptionLog.PullRequestNumber = input.PullRequestNumber;
        exceptionLog.PullRequestTitle = input.PullRequestTitle;
        exceptionLog.TicketReference = input.TicketReference;
    }

    // Same error (type + message + source + environment) recurring on the same UTC calendar day
    // is only persisted once; later occurrences bump OccurrenceCount instead of adding new rows.
    // A match on SourceFile + SourceLine is also treated as a duplicate on its own, since the
    // same line throwing is the same error even if the message text varies slightly.
    private async Task<ExceptionLog?> FindTodaysDuplicateAsync(CreateExceptionLogDto input)
    {
        var todayStart = DateTime.UtcNow.Date;
        var todayEnd = todayStart.AddDays(1);

        var queryable = await exceptionLogRepository.GetQueryableAsync();

        return await AsyncExecuter.FirstOrDefaultAsync(
            queryable
                .Where(x => x.CreationTime >= todayStart && x.CreationTime < todayEnd)
                .Where(x =>
                    (x.Source == input.Source &&
                     x.Environment == input.Environment &&
                     x.ExceptionType == input.ExceptionType &&
                     x.ExceptionMessage == input.ExceptionMessage) ||
                    (input.SourceFile != null && input.SourceLine != null &&
                     x.SourceFile == input.SourceFile && x.SourceLine == input.SourceLine)));
    }

    [Authorize(IdentityConsts.ITOperationsPolicyName)]
    public virtual async Task<PagedResultDto<ExceptionLogDto>> GetListAsync(GetExceptionLogsInput input)
    {
        // IT operations need visibility across all tenants, including host-side/background
        // exceptions that have no TenantId — the default multi-tenancy filter would otherwise
        // hide those rows whenever this is called from within a tenant context.
        using (dataFilter.Disable<IMultiTenant>())
        {
            var query = await exceptionLogRepository.GetQueryableAsync();

            if (input.FromDate.HasValue)
            {
                query = query.Where(x => x.CreationTime >= input.FromDate.Value);
            }

            if (input.ToDate.HasValue)
            {
                query = query.Where(x => x.CreationTime <= input.ToDate.Value);
            }

            if (input.Severity.HasValue)
            {
                query = query.Where(x => x.Severity == input.Severity.Value);
            }

            if (!string.IsNullOrWhiteSpace(input.Filter))
            {
                var filter = input.Filter.Trim();
                query = query.Where(x =>
                    x.Title.Contains(filter) ||
                    x.Message.Contains(filter) ||
                    (x.Source != null && x.Source.Contains(filter)) ||
                    (x.ExceptionType != null && x.ExceptionType.Contains(filter)) ||
                    (x.CorrelationId != null && x.CorrelationId.Contains(filter)) ||
                    (x.TicketReference != null && x.TicketReference.Contains(filter)) ||
                    (x.BlameAuthor != null && x.BlameAuthor.Contains(filter)) ||
                    (x.UserName != null && x.UserName.Contains(filter)) ||
                    (x.TenantName != null && x.TenantName.Contains(filter)));
            }

            var totalCount = await AsyncExecuter.CountAsync(query);

            query = query
                .OrderBy(string.IsNullOrWhiteSpace(input.Sorting) ? "CreationTime DESC" : input.Sorting)
                .Skip(input.SkipCount)
                .Take(input.MaxResultCount);

            var logs = await AsyncExecuter.ToListAsync(query);

            var items = logs.Select(MapToDto).ToList();

            return new PagedResultDto<ExceptionLogDto>(totalCount, items);
        }
    }

    private static ExceptionLogDto MapToDto(ExceptionLog log) => new()
    {
        Id = log.Id,
        CreationTime = log.CreationTime,
        TenantId = log.TenantId,
        UserId = log.UserId,
        UserName = log.UserName,
        TenantName = log.TenantName,
        NotificationType = log.NotificationType,
        Channel = log.Channel,
        Severity = log.Severity,
        Title = log.Title,
        Message = log.Message,
        Source = log.Source,
        SourceReference = log.SourceReference,
        OccurrenceCount = log.OccurrenceCount,
        CorrelationId = log.CorrelationId,
        ExceptionType = log.ExceptionType,
        ExceptionMessage = log.ExceptionMessage,
        StackExcerpt = log.StackExcerpt,
        SourceFile = log.SourceFile,
        SourceLine = log.SourceLine,
        CommitSha = log.CommitSha,
        Environment = log.Environment,
        BlameAuthor = log.BlameAuthor,
        BlameEmail = log.BlameEmail,
        BlameCommitSha = log.BlameCommitSha,
        BlameCommitMessage = log.BlameCommitMessage,
        PullRequestUrl = log.PullRequestUrl,
        PullRequestNumber = log.PullRequestNumber,
        PullRequestTitle = log.PullRequestTitle,
        TicketReference = log.TicketReference
    };
}
