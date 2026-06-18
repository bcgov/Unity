using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Unity.GrantManager.Applications;
using Unity.Notifications.Emails;
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
    IRepository<Applicant, Guid> applicantRepository)
    : ApplicationService, INotificationListAppService
{
    public virtual async Task<PagedResultDto<NotificationSummaryDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var sorting = string.IsNullOrWhiteSpace(input.Sorting) ? "SentDateTime DESC" : input.Sorting;

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
                EmailType = log.EmailType
            };
        }).ToList();

        return new PagedResultDto<NotificationSummaryDto>(totalCount, items);
    }
}
