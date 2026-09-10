using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Unity.Notifications.EmailNotifications;
using Unity.Notifications.Permissions;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Users;

namespace Unity.GrantManager.Web.Controllers;

[Authorize(NotificationsPermissions.NotificationList.View)]
[Route("Notifications")]
public class NotificationsController(
    ICurrentUser currentUser,
    IEmailNotificationService emailNotificationService) : AbpController
{
    [HttpGet("EmailModal")]
    public async Task<IActionResult> EmailModal(Guid applicationId, Guid emailId)
    {
        if (applicationId == Guid.Empty || emailId == Guid.Empty)
        {
            return BadRequest();
        }

        var selectedEmail = (await emailNotificationService.GetHistoryByApplicationId(applicationId))
            .SingleOrDefault(email => email.Id == emailId);

        if (selectedEmail is null)
        {
            return NotFound();
        }

        return View("EmailModal", new NotificationEmailModalViewModel
        {
            ApplicationId = applicationId,
            CurrentUserId = currentUser.Id ?? Guid.Empty,
            SelectedEmail = selectedEmail
        });
    }
}