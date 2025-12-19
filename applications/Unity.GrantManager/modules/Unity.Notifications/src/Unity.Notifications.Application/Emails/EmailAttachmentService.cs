using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.Notifications.Emails;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Users;

namespace Unity.Notifications.EmailNotifications;

public class EmailAttachmentService : ITransientDependency
{
    private readonly AmazonS3Client _amazonS3Client;
    private readonly IEmailLogAttachmentRepository _emailLogAttachmentRepository;
    private readonly IConfiguration _configuration;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<EmailAttachmentService> _logger;

    public EmailAttachmentService(
        IConfiguration configuration,
        IEmailLogAttachmentRepository emailLogAttachmentRepository,
        ICurrentUser currentUser,
        ILogger<EmailAttachmentService> logger)
    {
        _configuration = configuration;
        _emailLogAttachmentRepository = emailLogAttachmentRepository;
        _currentUser = currentUser;
        _logger = logger;

        // Initialize S3 client (same pattern as S3BlobProvider)
        var s3Config = new AmazonS3Config
        {
            RegionEndpoint = null,
            ServiceURL = configuration["S3:Endpoint"],
            AllowAutoRedirect = true,
            ForcePathStyle = true
        };

        _amazonS3Client = new AmazonS3Client(
            configuration["S3:AccessKeyId"],
            configuration["S3:SecretAccessKey"],
            s3Config
        );
    }

    public async Task<EmailLogAttachment> UploadAttachmentAsync(
        Guid emailLogId,
        Guid? tenantId,
        string fileName,
        byte[] fileContent,
        string contentType)
    {
        var s3Key = BuildS3Key(tenantId, emailLogId, fileName);
        var bucket = _configuration["S3:Bucket"];

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
            "Uploaded email attachment to S3: EmailLogId={EmailLogId}, FileName={FileName}, FileSize={FileSize}, S3Key={S3Key}",
            emailLogId, fileName, fileContent.Length, s3Key);

        // Create metadata record
        var attachment = new EmailLogAttachment
        {
            EmailLogId = emailLogId,
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

    public async Task<byte[]?> DownloadAttachmentAsync(Guid attachmentId)
    {
        var attachment = await _emailLogAttachmentRepository.GetAsync(attachmentId);
        if (attachment == null)
        {
            _logger.LogWarning("Attachment {AttachmentId} not found", attachmentId);
            return null;
        }

        return await DownloadFromS3Async(attachment.S3ObjectKey);
    }

    public async Task<byte[]?> DownloadFromS3Async(string s3ObjectKey)
    {
        var bucket = _configuration["S3:Bucket"];

        var getObjectRequest = new GetObjectRequest
        {
            BucketName = bucket,
            Key = s3ObjectKey
        };

        using var response = await _amazonS3Client.GetObjectAsync(getObjectRequest);
        using var memoryStream = new MemoryStream();
        await response.ResponseStream.CopyToAsync(memoryStream);

        _logger.LogInformation(
            "Downloaded email attachment from S3: S3Key={S3Key}, FileSize={FileSize}",
            s3ObjectKey, memoryStream.Length);
        return memoryStream.ToArray();
    }

    public async Task<List<EmailLogAttachment>> GetAttachmentsAsync(Guid emailLogId)
    {
        return await _emailLogAttachmentRepository.GetByEmailLogIdAsync(emailLogId);
    }

    private static string BuildS3Key(Guid? tenantId, Guid emailLogId, string fileName)
    {
        var basePath = "Email/FSB-AP-Payments";
        var tenantPart = tenantId?.ToString() ?? "host";
        var escapedFileName = Uri.EscapeDataString(fileName);

        return $"{basePath}/{tenantPart}/{emailLogId}/{escapedFileName}";
    }
}
