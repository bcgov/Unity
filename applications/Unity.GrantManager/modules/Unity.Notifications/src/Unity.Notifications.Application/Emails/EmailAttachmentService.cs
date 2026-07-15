using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unity.Notifications.Emails;
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
            UserId = _currentUser.Id ?? Guid.Empty,
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
}
