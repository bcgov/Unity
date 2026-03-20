using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Notifications.EmailNotifications;
using Unity.Notifications.Permissions;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Users;

namespace Unity.Notifications.Emails;

[Authorize(NotificationsPermissions.Email.Send)]
[ExposeServices(typeof(EmailLogAttachmentAppService), typeof(IEmailLogAttachmentAppService), typeof(IEmailLogAttachmentUploadService))]
public class EmailLogAttachmentAppService(
    IEmailLogAttachmentRepository emailLogAttachmentRepository,
    IEmailLogsRepository emailLogsRepository,
    EmailAttachmentService emailAttachmentService,
    IExternalUserLookupServiceProvider externalUserLookupServiceProvider) : ApplicationService, IEmailLogAttachmentAppService, IEmailLogAttachmentUploadService
{
    public async Task<List<EmailLogAttachmentDto>> GetListByEmailLogIdAsync(Guid emailLogId)
    {
        var attachments = await emailLogAttachmentRepository.GetByEmailLogIdAsync(emailLogId);
        var dtos = new List<EmailLogAttachmentDto>();

        foreach (var attachment in attachments)
        {
            var dto = new EmailLogAttachmentDto
            {
                Id = attachment.Id,
                FileName = attachment.FileName,
                DisplayName = attachment.DisplayName,
                Time = attachment.Time,
                FileSize = attachment.FileSize,
                ContentType = attachment.ContentType,
                S3ObjectKey = attachment.S3ObjectKey,
                AttachedBy = await ResolveUserNameAsync(attachment.UserId)
            };
            dtos.Add(dto);
        }

        return dtos;
    }

    public async Task DeleteAsync(Guid id)
    {
        var attachment = await emailLogAttachmentRepository.GetAsync(id);

        var emailLog = await emailLogsRepository.GetAsync(attachment.EmailLogId);
        if (emailLog.Status != EmailStatus.Draft)
        {
            throw new UserFriendlyException("Attachments can only be deleted from draft emails.");
        }

        try
        {
            await emailAttachmentService.DeleteFromS3Async(attachment.S3ObjectKey);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to delete S3 object {S3ObjectKey} for attachment {AttachmentId}", attachment.S3ObjectKey, id);
        }
        await emailLogAttachmentRepository.DeleteAsync(id);
    }

    public async Task<EmailLogAttachmentDto> UploadAsync(Guid emailLogId, Guid? tenantId, string fileName, byte[] content, string contentType)
    {
        var attachment = await emailAttachmentService.UploadUserAttachmentAsync(emailLogId, tenantId, fileName, content, contentType);

        return new EmailLogAttachmentDto
        {
            Id = attachment.Id,
            FileName = attachment.FileName,
            DisplayName = attachment.DisplayName,
            Time = attachment.Time,
            FileSize = attachment.FileSize,
            ContentType = attachment.ContentType,
            S3ObjectKey = attachment.S3ObjectKey,
            AttachedBy = await ResolveUserNameAsync(attachment.UserId)
        };
    }

    private async Task<string> ResolveUserNameAsync(Guid userId)
    {
        try
        {
            var user = await externalUserLookupServiceProvider.FindByIdAsync(userId);
            if (user == null) return string.Empty;

            var fullName = $"{user.Name} {user.Surname}".Trim();
            return string.IsNullOrEmpty(fullName) ? user.UserName : fullName;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to resolve username for UserId {UserId}", userId);
            return string.Empty;
        }
    }
}
