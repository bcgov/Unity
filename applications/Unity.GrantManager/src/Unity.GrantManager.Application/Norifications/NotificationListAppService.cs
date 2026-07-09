using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Localization;
using Unity.GrantManager.Applications;
using Unity.Notifications.Emails;
using Unity.Notifications.Localization;
using Unity.Notifications.Permissions;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Features;

namespace Unity.GrantManager.Notifications;

[Authorize(NotificationsPermissions.NotificationList.View)]
[RequiresFeature("Unity.Notifications")]
public class NotificationListAppService(
    IEmailLogsRepository emailLogsRepository,
    IRepository<Application, Guid> applicationRepository,
    IRepository<Applicant, Guid> applicantRepository,
    IStringLocalizer<NotificationsResource> notificationsLocalizer)
    : ApplicationService, INotificationListAppService
{
    private static readonly TimeZoneInfo VancouverTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");

    public virtual async Task<PagedResultDto<NotificationSummaryDto>> GetListAsync(NotificationListInputDto input)
    {
        var sorting = ResolveSorting(input.Sorting);
        var (fromUtc, toUtc) = ConvertToUtcRange(input.DateFrom, input.DateTo);

        var query = await emailLogsRepository.GetQueryableAsync();

        // Filter on the sent date, falling back to the creation date for rows that were never
        // sent (drafts, failures, scheduled emails). Otherwise their null SentDateTime would
        // drop them from every window except "All time".
        if (fromUtc.HasValue)
        {
            query = query.Where(e => (e.SentDateTime ?? e.CreationTime) >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(e => (e.SentDateTime ?? e.CreationTime) <= toUtc.Value);
        }

        query = query.OrderBy(sorting);

        // The list runs client-side (serverSideEnabled is off in Index.js): return every row in the
        // date window and let DataTables page, search and sort in the browser. TotalCount is the full
        // match count, not a page. This mirrors the Applications list, whose repository likewise does
        // not apply skip/take in client-side mode (GetApplicationListRecordsAsync) and sets
        // totalCount = items.Count to avoid a redundant count query. The 6-month default bounds the
        // payload; full server-side paging is a future option if volume ever requires it.
        var logs = await AsyncExecuter.ToListAsync(query);
        var totalCount = logs.Count;

        var applicationIds = logs.Select(l => l.ApplicationId).Distinct().ToArray();
        var applications = await applicationRepository.GetListAsync(a => applicationIds.Contains(a.Id));
        var referenceByAppId = applications.ToDictionary(a => a.Id, a => a.ReferenceNo);
        var applicantIdByAppId = applications.ToDictionary(a => a.Id, a => a.ApplicantId);

        var applicantIds = logs.Select(l => l.ApplicantId)
            .Concat(applicantIdByAppId.Values)
            .Distinct()
            .ToArray();
        var applicants = await applicantRepository.GetListAsync(a => applicantIds.Contains(a.Id));
        var applicantNameById = applicants.ToDictionary(a => a.Id, a => a.ApplicantName ?? string.Empty);

        var items = logs.Select(log =>
        {
            referenceByAppId.TryGetValue(log.ApplicationId, out var referenceNo);

            // Prefer the EmailLog.ApplicantId; fall back to the application's applicant.
            var applicantId = log.ApplicantId != Guid.Empty
                ? log.ApplicantId
                : applicantIdByAppId.GetValueOrDefault(log.ApplicationId);
            applicantNameById.TryGetValue(applicantId, out var applicantName);

            return new NotificationSummaryDto
            {
                Id = log.Id,
                ApplicationId = log.ApplicationId,
                SubmissionReferenceNo = referenceNo ?? string.Empty,
                ApplicantName = applicantName ?? string.Empty,
                SentDateTime = log.SentDateTime,
                Status = log.Status,
                FromAddress = log.FromAddress,
                ToAddress = log.ToAddress,
                Subject = log.Subject,
                Recipient = log.Recipient,
                EmailType = log.EmailType,
                EmailTypeText = GetEmailTypeText(log.EmailType)
            };
        }).ToList();

        return new PagedResultDto<NotificationSummaryDto>(totalCount, items);
    }

    // Converts a Vancouver-local date range (date-only) to an inclusive UTC range:
    // From -> start of local day, To -> end of local day (23:59:59.9999999). Mirrors ApplicationRepository.
    private static (DateTime? FromUtc, DateTime? ToUtc) ConvertToUtcRange(DateTime? fromLocal, DateTime? toLocal)
    {
        DateTime? fromUtc = null;
        DateTime? toUtc = null;

        if (fromLocal.HasValue)
        {
            var localFrom = DateTime.SpecifyKind(fromLocal.Value.Date, DateTimeKind.Unspecified);
            fromUtc = TimeZoneInfo.ConvertTimeToUtc(localFrom, VancouverTimeZone);
        }

        if (toLocal.HasValue)
        {
            var localToEndOfDay = DateTime.SpecifyKind(toLocal.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Unspecified);
            toUtc = TimeZoneInfo.ConvertTimeToUtc(localToEndOfDay, VancouverTimeZone);
        }

        return (fromUtc, toUtc);
    }

    // Maps the EmailType enum to the user-facing label shown on the Notification List
    private string GetEmailTypeText(EmailType? emailType)
    {
        return emailType switch
        {
            EmailType.Manual => notificationsLocalizer["NotificationList:EmailType:Manual"].Value,
            EmailType.DateBased => notificationsLocalizer["NotificationList:EmailType:Scheduled"].Value,
            EmailType.EventBased => notificationsLocalizer["NotificationList:EmailType:EventBased"].Value,
            EmailType.Delayed => notificationsLocalizer["NotificationList:EmailType:Delayed"].Value,
            // Fallback for any future unmapped value: show the raw enum name rather than a blank cell.
            _ => emailType?.ToString() ?? string.Empty
        };
    }

    private const string DefaultSorting = "SentDateTime DESC";

    // Only EmailLog-backed columns can be sorted at the database level. Joined columns
    // (Submission Id, Applicant Name) are not sortable server-side; any unrecognized
    // value falls back to the default so client input is never forwarded raw into the
    // dynamic-LINQ ordering.
    private static readonly HashSet<string> SortableColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(EmailLog.SentDateTime),
        nameof(EmailLog.Status),
        nameof(EmailLog.FromAddress),
        nameof(EmailLog.ToAddress),
        nameof(EmailLog.Subject),
        nameof(EmailLog.Recipient),
        nameof(EmailLog.EmailType)
    };

    private static string ResolveSorting(string? requested)
    {
        if (string.IsNullOrWhiteSpace(requested))
        {
            return DefaultSorting;
        }

        var parts = requested.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var column = parts[0].TrimEnd(',');

        if (!SortableColumns.TryGetValue(column, out var canonicalColumn))
        {
            return DefaultSorting;
        }

        var descending = parts.Length > 1 && parts[1].StartsWith("DESC", StringComparison.OrdinalIgnoreCase);
        return $"{canonicalColumn} {(descending ? "DESC" : "ASC")}";
    }
}
