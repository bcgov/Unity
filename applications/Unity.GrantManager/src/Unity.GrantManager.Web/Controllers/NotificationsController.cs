using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Unity.Notifications.EmailNotifications;
using Unity.Notifications.Emails;
using Unity.Notifications.Permissions;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Users;

namespace Unity.GrantManager.Web.Controllers;

[Authorize(NotificationsPermissions.NotificationList.View)]
[Route("Notifications")]
public class NotificationsController(
    ICurrentUser currentUser,
    IEmailNotificationService emailNotificationService,
    ICurrentTenant currentTenant) : AbpController
{
    [HttpGet("EmailModal")]
    public async Task<IActionResult> EmailModal(Guid applicationId, Guid emailId)
    {
        if (emailId == Guid.Empty)
        {
            return BadRequest();
        }

        // The stored email determines its context; applicant notifications have no application.
        var emailLog = await emailNotificationService.GetEmailLogById(emailId);
        if (emailLog is null || emailLog.TenantId != currentTenant.Id
            || (applicationId != Guid.Empty && applicationId != emailLog.ApplicationId))
        {
            return NotFound();
        }

        if (emailLog.ApplicationId == Guid.Empty)
        {
            if (emailLog.ApplicantId == Guid.Empty)
            {
                return NotFound();
            }

            return View("ApplicantEmailModal", new NotificationEmailModalViewModel
            {
                ApplicantId = emailLog.ApplicantId,
                TenantId = emailLog.TenantId,
                BodyType = emailLog.BodyType,
                SelectedEmail = new EmailHistoryDto
                {
                    Id = emailLog.Id,
                    Subject = emailLog.Subject,
                    Status = emailLog.Status,
                    FromAddress = emailLog.FromAddress,
                    ToAddress = emailLog.ToAddress,
                    Cc = emailLog.CC,
                    Bcc = emailLog.BCC,
                    SentDateTime = emailLog.SentDateTime,
                    Body = emailLog.Body
                }
            });
        }

        var selectedEmail = (await emailNotificationService.GetHistoryByApplicationId(emailLog.ApplicationId))
            .SingleOrDefault(email => email.Id == emailId);

        if (selectedEmail is null)
        {
            return NotFound();
        }

        return View("EmailModal", new NotificationEmailModalViewModel
        {
            ApplicationId = emailLog.ApplicationId,
            CurrentUserId = currentUser.Id ?? Guid.Empty,
            SelectedEmail = selectedEmail
        });
    }
}
