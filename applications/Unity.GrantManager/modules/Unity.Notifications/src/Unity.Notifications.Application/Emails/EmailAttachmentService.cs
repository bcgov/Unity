using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Unity.Notifications.Emails;
using Volo.Abp.Authorization;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Users;

namespace Unity.Notifications.EmailNotifications;

public class EmailAttachmentService : ITransientDependency
{
    private const string S3BucketConfigKey = "S3:Bucket";

    private readonly IAmazonS3 _amazonS3Client;
    private readonly IEmailLogAttachmentRepository _emailLogAttachmentRepository;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<EmailAttachmentService> _logger;

    public EmailAttachmentService(
        IConfiguration configuration,
        IEmailLogAttachmentRepository emailLogAttachmentRepository,
        ICurrentUser currentUser,
        ILogger<EmailAttachmentService> logger,
        IAmazonS3 amazonS3Client)
    {
        _configuration = configuration;
        _emailLogAttachmentRepository = emailLogAttachmentRepository;
        _currentUser = currentUser;
        _logger = logger;
        _amazonS3Client = amazonS3Client;
    }

    public async Task<EmailLogAttachment> UploadAttachmentAsync(
        Guid? emailLogId,
        Guid? templateId,
        Guid? tenantId,
        string fileName,
        byte[] fileContent,
        string contentType)
    {
        var guid = emailLogId ?? templateId ?? throw new ArgumentException("Either emailLogId or templateId must be provided.");
        var s3Key = BuildS3Key(tenantId, guid, fileName);
        var bucket = _configuration[S3BucketConfigKey];

        // Upload to S3
        using var uploadStream = new MemoryStream(fileContent);
        var putRequest = new PutObjectRequest
        {
            BucketName = bucket,
            Key = s3Key,
            ContentType = contentType,
            InputStream = uploadStream,
            UseChunkEncoding = false,
            DisablePayloadSigning = false
        };

        await _amazonS3Client.PutObjectAsync(putRequest);
        _logger.LogInformation(
            "Uploaded email attachment to S3: FileName={FileName}, FileSize={FileSize}",
            fileName, fileContent.Length);

        // Create metadata record
        var attachment = new EmailLogAttachment
        {
            EmailLogId = emailLogId,
            TemplateId = templateId,
            S3ObjectKey = s3Key,
            FileName = fileName,
            DisplayName = fileName,
            ContentType = contentType,
            FileSize = fileContent.Length,
            Time = DateTime.UtcNow,
            // Unlike UploadUserAttachmentAsync below, this path is reached from
            // EmailNotificationHandler - a local event handler that can run for
            // system/schedule-triggered emails with no interactive user in context, so a missing
            // ICurrentUser.Id here isn't necessarily an error condition. The caller already wraps
            // this in a try/catch that logs and sends the email without the attachment on any
            // failure, so Guid.Empty (rather than throwing) is the intentional "no user" marker.
            UserId = _currentUser.Id ?? Guid.Empty,
            TenantId = tenantId
        };

        await _emailLogAttachmentRepository.InsertAsync(attachment);
        return attachment;
    }

    public async Task<byte[]?> DownloadFromS3Async(string s3ObjectKey)
    {
        var bucket = _configuration[S3BucketConfigKey];

        var getObjectRequest = new GetObjectRequest
        {
            BucketName = bucket,
            Key = s3ObjectKey
        };

        using var response = await _amazonS3Client.GetObjectAsync(getObjectRequest);
        using var memoryStream = new MemoryStream();
        await response.ResponseStream.CopyToAsync(memoryStream);

        _logger.LogInformation(
            "Downloaded email attachment from S3");
        return memoryStream.ToArray();
    }

    public async Task<byte[]?> DownloadAttachmentFromS3Async(EmailLogAttachment attachment)
    {
        try
        {
            return await DownloadFromS3Async(attachment.S3ObjectKey);
        }
        catch (AmazonS3Exception ex) when (IsMissingObject(ex))
        {
            throw new MissingEmailAttachmentsException(
                [GetAttachmentName(attachment)],
                EmailAttachmentValidationContext.Email);
        }
    }

    public async Task<EmailLogAttachment> UploadUserAttachmentAsync(
        Guid? emailLogId,
        Guid? templateId,
        Guid? tenantId,
        string fileName,
        byte[] fileContent,
        string contentType)
    {
        var uniqueKey = Guid.NewGuid();
        Guid generateGuid = emailLogId ?? templateId ?? throw new ArgumentException("Either emailLogId or templateId must be provided.");
        var s3Key = BuildUserAttachmentS3Key(tenantId, generateGuid, uniqueKey, fileName);
        var bucket = _configuration[S3BucketConfigKey];

        using var uploadStream = new MemoryStream(fileContent);
        var putRequest = new PutObjectRequest
        {
            BucketName = bucket,
            Key = s3Key,
            ContentType = contentType,
            InputStream = uploadStream,
            UseChunkEncoding = false,
            DisablePayloadSigning = false
        };

        await _amazonS3Client.PutObjectAsync(putRequest);
        _logger.LogInformation(
            "Uploaded user email attachment to S3: FileName={FileName}, FileSize={FileSize}",
            fileName, fileContent.Length);

        var attachment = new EmailLogAttachment
        {
            EmailLogId = emailLogId,
            TemplateId = templateId,
            S3ObjectKey = s3Key,
            FileName = fileName,
            DisplayName = fileName,
            ContentType = contentType,
            FileSize = fileContent.Length,
            Time = DateTime.UtcNow,
            // A missing ICurrentUser.Id means this was reached without an authenticated user -
            // fail loudly rather than silently attributing the attachment to Guid.Empty, which
            // would look like a valid, specific user rather than an error state.
            UserId = _currentUser.Id ?? throw new AbpAuthorizationException("Cannot save an email attachment without an authenticated user."),
            TenantId = tenantId
        };

        await _emailLogAttachmentRepository.InsertAsync(attachment);
        return attachment;
    }

    public async Task DeleteFromS3Async(string s3ObjectKey)
    {
        var bucket = _configuration[S3BucketConfigKey];
        var deleteRequest = new DeleteObjectRequest
        {
            BucketName = bucket,
            Key = s3ObjectKey
        };
        await _amazonS3Client.DeleteObjectAsync(deleteRequest);
        _logger.LogInformation("Deleted email attachment from S3.");
    }

    public async Task<List<EmailLogAttachment>> GetAttachmentsAsync(Guid emailLogId)
    {
        return await _emailLogAttachmentRepository.GetByEmailLogIdAsync(emailLogId);
    }

    public async Task ValidateEmailAttachmentsAsync(Guid emailLogId)
    {
        var attachments = await GetAttachmentsAsync(emailLogId);
        await ValidateAttachmentsExistAsync(attachments, EmailAttachmentValidationContext.Email);
    }

    public async Task ValidateAttachmentsExistAsync(
        IEnumerable<EmailLogAttachment> attachments,
        EmailAttachmentValidationContext context)
    {
        var missingFiles = new List<string>();
        var bucket = _configuration[S3BucketConfigKey];

        foreach (var attachment in attachments)
        {
            try
            {
                await _amazonS3Client.GetObjectMetadataAsync(new GetObjectMetadataRequest
                {
                    BucketName = bucket,
                    Key = attachment.S3ObjectKey
                });
            }
            catch (AmazonS3Exception ex) when (IsMissingObject(ex))
            {
                missingFiles.Add(GetAttachmentName(attachment));
            }
        }

        if (missingFiles.Count != 0)
        {
            throw new MissingEmailAttachmentsException(missingFiles, context);
        }
    }

    public async Task<int> CopyTemplateAttachmentsAsync(Guid templateId, Guid emailLogId, Guid? tenantId)
    {
        var templateAttachments = await _emailLogAttachmentRepository.GetByTemplateIdAsync(templateId);
        var existingAttachments = await _emailLogAttachmentRepository.GetByEmailLogIdAsync(emailLogId);
        // Dedup by (FileName, FileSize, ContentType) rather than S3ObjectKey: each copy gets its own
        // S3 object (see below), so a re-run of this method for the same emailLogId/templateId would
        // never see a matching key even though the attachment was already copied.
        var alreadyCopied = existingAttachments
            .Where(a => a.OriginTemplateId == templateId)
            .Select(a => (a.FileName, a.FileSize, a.ContentType))
            .ToHashSet();

        var attachmentsToCopy = templateAttachments
            .Where(attachment => !alreadyCopied.Contains(
                (attachment.FileName, attachment.FileSize, attachment.ContentType)))
            .ToList();

        return await CopyAttachmentsAsync(templateId, emailLogId, tenantId, attachmentsToCopy);
    }

    public async Task<int> ReplaceTemplateAttachmentsAsync(Guid templateId, Guid emailLogId, Guid? tenantId)
    {
        var templateAttachments = await _emailLogAttachmentRepository.GetByTemplateIdAsync(templateId);
        var previousTemplateAttachments = await _emailLogAttachmentRepository
            .GetOriginAttachmentsByEmailLogIdAsync(emailLogId);

        // Validate every source before changing the draft. This also catches orphaned template
        // metadata when the same template is reapplied to an email that already has matching rows.
        await ValidateAttachmentsExistAsync(
            templateAttachments,
            EmailAttachmentValidationContext.Template);

        // A template replacement always gets fresh, email-owned S3 objects. Only after all new
        // objects and rows exist do we remove the previous template-origin attachments. Manually
        // uploaded draft attachments have no OriginTemplateId and are intentionally left alone.
        var copiedCount = await CopyAttachmentsAsync(
            templateId,
            emailLogId,
            tenantId,
            templateAttachments,
            sourcesAlreadyValidated: true);

        foreach (var previousAttachment in previousTemplateAttachments)
        {
            await DeleteAttachmentAsync(previousAttachment);
        }

        return copiedCount;
    }

    private async Task<int> CopyAttachmentsAsync(
        Guid templateId,
        Guid emailLogId,
        Guid? tenantId,
        IReadOnlyCollection<EmailLogAttachment> attachmentsToCopy,
        bool sourcesAlreadyValidated = false)
    {
        if (!sourcesAlreadyValidated)
        {
            await ValidateAttachmentsExistAsync(
                attachmentsToCopy,
                EmailAttachmentValidationContext.Template);
        }

        var bucket = _configuration[S3BucketConfigKey];
        var copiedS3Keys = new List<string>();
        var copiedAttachments = new List<EmailLogAttachment>();

        try
        {
            foreach (var templateAttachment in attachmentsToCopy)
            {
                var copiedS3Key = BuildUserAttachmentS3Key(
                    tenantId,
                    emailLogId,
                    Guid.NewGuid(),
                    GetAttachmentName(templateAttachment));

                try
                {
                    await _amazonS3Client.CopyObjectAsync(new CopyObjectRequest
                    {
                        SourceBucket = bucket,
                        SourceKey = templateAttachment.S3ObjectKey,
                        DestinationBucket = bucket,
                        DestinationKey = copiedS3Key
                    });
                }
                catch (AmazonS3Exception ex) when (IsMissingObject(ex))
                {
                    throw new MissingEmailAttachmentsException(
                        [GetAttachmentName(templateAttachment)],
                        EmailAttachmentValidationContext.Template);
                }

                copiedS3Keys.Add(copiedS3Key);
                copiedAttachments.Add(new EmailLogAttachment
                {
                    EmailLogId = emailLogId,
                    TemplateId = null,
                    OriginTemplateId = templateId,
                    S3ObjectKey = copiedS3Key,
                    FileName = templateAttachment.FileName,
                    DisplayName = templateAttachment.DisplayName,
                    ContentType = templateAttachment.ContentType,
                    FileSize = templateAttachment.FileSize,
                    Time = DateTime.UtcNow,
                    UserId = _currentUser.Id ?? Guid.Empty,
                    TenantId = tenantId
                });
            }

            if (copiedAttachments.Count != 0)
            {
                await _emailLogAttachmentRepository.InsertManyAsync(copiedAttachments, autoSave: true);
            }

            return copiedAttachments.Count;
        }
        catch
        {
            await DeleteCopiedObjectsBestEffortAsync(copiedS3Keys);
            throw;
        }
    }

    public async Task<int> DeleteOriginAttachmentsAsync(Guid emailLogId)
    {
        var attachments = await _emailLogAttachmentRepository.GetOriginAttachmentsByEmailLogIdAsync(emailLogId);
        foreach (var attachment in attachments)
        {
            await DeleteAttachmentAsync(attachment);
        }

        return attachments.Count;
    }

    public async Task DeleteAttachmentAsync(EmailLogAttachment attachment)
    {
        var hasOtherReferences = await _emailLogAttachmentRepository.HasOtherReferencesAsync(
            attachment.S3ObjectKey,
            attachment.Id);

        try
        {
            await _emailLogAttachmentRepository.DeleteAsync(attachment, autoSave: true);
        }
        catch (Volo.Abp.Domain.Entities.EntityNotFoundException)
        {
            return;
        }

        if (hasOtherReferences)
        {
            _logger.LogInformation(
                "Preserved shared S3 attachment object while deleting metadata for attachment {AttachmentId}.",
                attachment.Id);
            return;
        }

        try
        {
            await DeleteFromS3Async(attachment.S3ObjectKey);
        }
        catch (Exception ex)
        {
            // The database must never retain a key merely because best-effort storage cleanup failed.
            // A leaked object is safer than deleting an object that another attachment still needs.
            _logger.LogError(
                ex,
                "Failed to delete unreferenced S3 object for attachment {AttachmentId}.",
                attachment.Id);
        }
    }

    public async Task<long> GetTotalFileSizeAsync(Guid? emailLogId, Guid? templateId)
    {
        if(emailLogId != null)
        {
            var attachments = await _emailLogAttachmentRepository.GetByEmailLogIdAsync(emailLogId.Value);
            return attachments.Sum(a => a.FileSize);
        }
        else if(templateId != null)
        {
            var attachments = await _emailLogAttachmentRepository.GetByTemplateIdAsync(templateId.Value);
            return attachments.Sum(a => a.FileSize);
        }
        else
        {
            throw new ArgumentException("Either emailLogId or templateId must be provided.");
        }
    }

    private static string BuildUserAttachmentS3Key(Guid? tenantId, Guid emailLogId, Guid attachmentId, string fileName)
    {
        var basePath = "Email/Attachments";
        var tenantPart = tenantId?.ToString() ?? "host";
        var escapedFileName = Uri.EscapeDataString(fileName);

        return $"{basePath}/{tenantPart}/{emailLogId}/{attachmentId}/{escapedFileName}";
    }

    private static string BuildS3Key(Guid? tenantId, Guid emailLogId, string fileName)
    {
        var basePath = "Email/FSB-AP-Payments";
        var tenantPart = tenantId?.ToString() ?? "host";
        var escapedFileName = Uri.EscapeDataString(fileName);

        return $"{basePath}/{tenantPart}/{emailLogId}/{escapedFileName}";
    }

    private async Task DeleteCopiedObjectsBestEffortAsync(IEnumerable<string> s3ObjectKeys)
    {
        foreach (var s3ObjectKey in s3ObjectKeys)
        {
            try
            {
                await DeleteFromS3Async(s3ObjectKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clean up copied S3 attachment object.");
            }
        }
    }

    private static bool IsMissingObject(AmazonS3Exception exception)
    {
        return exception.StatusCode == HttpStatusCode.NotFound
            || string.Equals(exception.ErrorCode, "NoSuchKey", StringComparison.OrdinalIgnoreCase)
            || string.Equals(exception.ErrorCode, "NotFound", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetAttachmentName(EmailLogAttachment attachment)
    {
        return attachment.FileName
            ?? attachment.DisplayName
            ?? "Unnamed attachment";
    }
}
