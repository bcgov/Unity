using System;
using System.Collections.Generic;
using System.Linq;
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
    public virtual async Task<PagedResultDto<NotificationSummaryDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var sorting = ResolveSorting(input.Sorting);

        var totalCount = await emailLogsRepository.GetCountAsync();
        var logs = await emailLogsRepository.GetPagedListAsync(input.SkipCount, input.MaxResultCount, sorting);

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
                EmailTypeText = GetEmailTypeText(log.EmailType)
            };
        }).ToList();

        return new PagedResultDto<NotificationSummaryDto>(totalCount, items);
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
            _ => string.Empty
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
