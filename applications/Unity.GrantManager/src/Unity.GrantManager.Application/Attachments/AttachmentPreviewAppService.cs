using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;

namespace Unity.GrantManager.Attachments;

public class AttachmentPreviewAppService : ApplicationService, IAttachmentPreviewAppService, ITransientDependency
{
    private readonly IFileAppService _fileAppService;
    private readonly ILibreOfficeConversionService _libreOfficeConversionService;
    private readonly AmazonS3Client _amazonS3Client;
    private readonly string _bucket;
    private readonly string _applicationFolder;
    private readonly string _assessmentFolder;
    private readonly string _applicantFolder;

    public AttachmentPreviewAppService(
        IFileAppService fileAppService,
        ILibreOfficeConversionService libreOfficeConversionService,
        IConfiguration configuration)
    {
        _fileAppService = fileAppService;
        _libreOfficeConversionService = libreOfficeConversionService;

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
            s3Config);

        _bucket = configuration["S3:Bucket"] ?? throw new InvalidOperationException("Missing server configuration: S3:Bucket");
        _applicationFolder = NormalizeFolder(configuration["S3:ApplicationS3Folder"] ?? throw new InvalidOperationException("Missing server configuration: S3:ApplicationS3Folder"));
        _assessmentFolder = NormalizeFolder(configuration["S3:AssessmentS3Folder"] ?? throw new InvalidOperationException("Missing server configuration: S3:AssessmentS3Folder"));
        _applicantFolder = NormalizeFolder(configuration["S3:ApplicantS3Folder"] ?? throw new InvalidOperationException("Missing server configuration: S3:ApplicantS3Folder"));
    }

    public async Task<BlobDto> GetOrCreatePreviewPdfAsync(AttachmentType attachmentType, Guid ownerId, string fileName)
    {
        var folder = attachmentType switch
        {
            AttachmentType.APPLICATION => _applicationFolder,
            AttachmentType.ASSESSMENT  => _assessmentFolder,
            AttachmentType.APPLICANT   => _applicantFolder,
            _ => throw new ArgumentException($"Unsupported attachment type for preview: {attachmentType}")
        };

        var originalKey = $"{folder}/{ownerId}/{fileName}";
        var previewKey  = $"{folder}/{ownerId}/preview/{fileName}.pdf";
        var previewName = fileName + ".pdf";
        var safeFileName = SanitizeForLog(fileName);
        var safePreviewKey = SanitizeForLog(previewKey);

        // Try S3 cache first
        var cached = await TryGetCachedPreviewAsync(previewKey, previewName);
        if (cached != null)
        {
            Logger.LogInformation("AttachmentPreviewAppService: serving cached preview for {FileName} [{AttachmentType}/{OwnerId}]", safeFileName, attachmentType, ownerId);
            return cached;
        }

        // Cache miss — download original, convert, cache, return
        Logger.LogInformation("AttachmentPreviewAppService: no cached preview found for {FileName} [{AttachmentType}/{OwnerId}] — starting LibreOffice conversion", safeFileName, attachmentType, ownerId);
        var original = await _fileAppService.GetBlobAsync(new GetBlobRequestDto { S3ObjectKey = originalKey, Name = fileName });
        var pdfBytes = await _libreOfficeConversionService.ConvertToPdfAsync(original.Content, fileName);
        await UploadPreviewAsync(previewKey, pdfBytes);
        Logger.LogInformation("AttachmentPreviewAppService: conversion complete for {FileName} — preview cached at {PreviewKey}", safeFileName, safePreviewKey);

        return new BlobDto { Content = pdfBytes, ContentType = "application/pdf", Name = previewName };
    }

    public async Task<BlobDto> GetOrCreateChefsPreviewPdfAsync(Guid formSubmissionId, Guid chefsFileId, string fileName, byte[] originalContent)
    {
        var previewKey  = $"chefs/{formSubmissionId}/{chefsFileId}/preview/{fileName}.pdf";
        var previewName = fileName + ".pdf";
        var safeFileNameForLog = SanitizeForLog(fileName);

        // Try S3 cache first
        var cached = await TryGetCachedPreviewAsync(previewKey, previewName);
        if (cached != null)
        {
            Logger.LogInformation("AttachmentPreviewAppService: serving cached preview for CHEFS file {FileName} [{FormSubmissionId}/{ChefsFileId}]", safeFileNameForLog, formSubmissionId, chefsFileId);
            return cached;
        }

        // Cache miss — convert provided content, cache, return
        Logger.LogInformation("AttachmentPreviewAppService: no cached preview found for CHEFS file {FileName} [{FormSubmissionId}/{ChefsFileId}] — starting LibreOffice conversion", safeFileNameForLog, formSubmissionId, chefsFileId);
        var pdfBytes = await _libreOfficeConversionService.ConvertToPdfAsync(originalContent, fileName);
        await UploadPreviewAsync(previewKey, pdfBytes);
        Logger.LogInformation("AttachmentPreviewAppService: conversion complete for CHEFS file {FileName} — preview cached at {PreviewKey}", safeFileNameForLog, previewKey);

        return new BlobDto { Content = pdfBytes, ContentType = "application/pdf", Name = previewName };
    }

    private async Task<BlobDto?> TryGetCachedPreviewAsync(string previewKey, string previewName)
    {
        try
        {
            var cached = await _fileAppService.GetBlobAsync(new GetBlobRequestDto { S3ObjectKey = previewKey, Name = previewName });
            if (cached?.Content?.Length > 0)
                return cached;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            // Cache miss — expected when preview has not been generated yet
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "AttachmentPreviewAppService: unexpected error checking preview cache for key {PreviewKey}", SanitizeForLog(previewKey));
        }
        return null;
    }

    private async Task UploadPreviewAsync(string previewKey, byte[] pdfBytes)
    {
        using var stream = new MemoryStream(pdfBytes);
        var putRequest = new PutObjectRequest
        {
            BucketName = _bucket,
            Key = EscapeKeyFileName(previewKey),
            ContentType = "application/pdf",
            InputStream = stream,
            UseChunkEncoding = false,
            DisablePayloadSigning = false
        };
        await _amazonS3Client.PutObjectAsync(putRequest);
    }

    private static string SanitizeForLog(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value
            .Replace("\r", string.Empty)
            .Replace("\n", string.Empty);
    }

    private static string NormalizeFolder(string folder)
        => folder.EndsWith('/') ? folder.TrimEnd('/') : folder;

    private static string SanitizeForLog(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value
            .Replace("\r", "\\r")
            .Replace("\n", "\\n");
    }

    private static string EscapeKeyFileName(string s3ObjectKey)
    {
        var lastSlash = s3ObjectKey.LastIndexOf('/');
        if (lastSlash < 0) return Uri.EscapeDataString(s3ObjectKey);
        return s3ObjectKey[..(lastSlash + 1)] + Uri.EscapeDataString(s3ObjectKey[(lastSlash + 1)..]);
    }
    private static string SanitizeForLog(string value)
    {
        return value?.Replace("\r", string.Empty).Replace("\n", string.Empty) ?? string.Empty;
    }
}
