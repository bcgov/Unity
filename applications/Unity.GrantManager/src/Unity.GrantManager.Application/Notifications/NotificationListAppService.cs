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
        TimeZoneInfo.FindSystemTimeZoneById("America/Vancouver");

    public virtual async Task<PagedResultDto<NotificationSummaryDto>> GetListAsync(NotificationListInputDto input)
    {
        var sorting = ResolveSorting(input.Sorting);
        var (fromUtc, toUtc) = ConvertToUtcRange(input.DateFrom, input.DateTo);

        var query = await emailLogsRepository.GetQueryableAsync();

        // Filter on the sent date, falling back to the creation date for rows that were never
        // sent (drafts, failures, scheduled emails). Otherwise their null SentDateTime would
        // drop them from every window except "All time".
        // Written as an OR of plain column comparisons rather than COALESCE(SentDateTime, CreationTime)
        // so the database can use the SentDateTime and CreationTime indexes.
        if (fromUtc.HasValue)
        {
            var from = fromUtc.Value;
            query = query.Where(e => e.SentDateTime >= from
                || (e.SentDateTime == null && e.CreationTime >= from));
        }

        if (toUtc.HasValue)
        {
            var to = toUtc.Value;
            query = query.Where(e => e.SentDateTime <= to
                || (e.SentDateTime == null && e.CreationTime <= to));
        }

        query = input.View switch
        {
            NotificationListView.Application => query.Where(e => e.ApplicationId != Guid.Empty),
            NotificationListView.Applicant => query.Where(e => e.ApplicationId == Guid.Empty),
            _ => query
        };

        // Project to the list columns only. EmailLog carries the full HTML Body, CC/BCC and the CHES
        // response; loading whole entities for every row in the window was the main cost of this page.
        var projected = query.OrderBy(sorting).Select(e => new NotificationSummaryDto
        {
            Id = e.Id,
            ApplicationId = e.ApplicationId,
            ApplicantId = e.ApplicantId,
            SentDateTime = e.SentDateTime,
            Status = e.Status,
            FromAddress = e.FromAddress,
            ToAddress = e.ToAddress,
            Subject = e.Subject,
            Recipient = e.Recipient,
            EmailType = e.EmailType
        });

        // The list runs client-side (serverSideEnabled is off in Index.js): return every row in the
        // date window and let DataTables page, search and sort in the browser. TotalCount is the full
        // match count, not a page. This mirrors the Applications list, whose repository likewise does
        // not apply skip/take in client-side mode (GetApplicationListRecordsAsync) and sets
        // totalCount = items.Count to avoid a redundant count query. The 6-month default bounds the
        // payload; full server-side paging is a future option if volume ever requires it.
        var items = await AsyncExecuter.ToListAsync(projected);
        var totalCount = items.Count;

        // EmailLog lives in NotificationsDbContext and Application/Applicant in GrantTenantDbContext,
        // so these cannot be one SQL join. Each lookup is a single projected query keyed by the
        // distinct ids on this result set.
        var applicationIds = items.Select(i => i.ApplicationId)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
        var applicationQuery = await applicationRepository.GetQueryableAsync();
        var applications = applicationIds.Length == 0
            ? new List<ApplicationLookup>()
            : await AsyncExecuter.ToListAsync(applicationQuery
                .Where(a => applicationIds.Contains(a.Id))
                .Select(a => new ApplicationLookup(a.Id, a.ReferenceNo, a.ApplicantId)));
        var applicationById = applications.ToDictionary(a => a.Id);

        var applicantIds = items.Select(i => i.ApplicantId)
            .Concat(applications.Select(a => a.ApplicantId))
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
        var applicantQuery = await applicantRepository.GetQueryableAsync();
        var applicantNameById = applicantIds.Length == 0
            ? new Dictionary<Guid, string>()
            : (await AsyncExecuter.ToListAsync(applicantQuery
                .Where(a => applicantIds.Contains(a.Id))
                .Select(a => new { a.Id, a.ApplicantName })))
                .ToDictionary(a => a.Id, a => a.ApplicantName ?? string.Empty);

        // Resolve each localized label once rather than once per row.
        var emailTypeTextCache = new Dictionary<EmailType, string>();
        string ResolveEmailTypeText(EmailType? emailType)
        {
            if (!emailType.HasValue)
            {
                return string.Empty;
            }

            if (!emailTypeTextCache.TryGetValue(emailType.Value, out var text))
            {
                text = GetEmailTypeText(emailType);
                emailTypeTextCache[emailType.Value] = text;
            }

            return text;
        }

        foreach (var item in items)
        {
            applicationById.TryGetValue(item.ApplicationId, out var application);

            // Prefer the EmailLog.ApplicantId; fall back to the application's applicant.
            if (item.ApplicantId == Guid.Empty && application is not null)
            {
                item.ApplicantId = application.ApplicantId;
            }

            item.SubmissionReferenceNo = application?.ReferenceNo ?? string.Empty;
            item.ApplicantName = applicantNameById.GetValueOrDefault(item.ApplicantId) ?? string.Empty;
            item.EmailTypeText = ResolveEmailTypeText(item.EmailType);
        }

        return new PagedResultDto<NotificationSummaryDto>(totalCount, items);
    }

    private sealed record ApplicationLookup(Guid Id, string? ReferenceNo, Guid ApplicantId);

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
