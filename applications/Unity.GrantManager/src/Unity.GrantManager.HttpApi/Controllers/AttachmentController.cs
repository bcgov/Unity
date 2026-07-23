using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Unity.GrantManager.Attachments;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.Models;
using Unity.Notifications.Emails;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Validation;

namespace Unity.GrantManager.Controllers
{
    internal static class LibreOfficeInstallationCache
    {
        private static int _hasCachedValue;
        private static bool _isInstalled;
        private static readonly object SyncRoot = new();

        public static bool IsInstalled(Func<bool> installationCheck)
        {
            if (Volatile.Read(ref _hasCachedValue) == 1)
            {
                return _isInstalled;
            }

            lock (SyncRoot)
            {
                if (_hasCachedValue == 0)
                {
                    _isInstalled = installationCheck();
                    Volatile.Write(ref _hasCachedValue, 1);
                }
            }

            return _isInstalled;
        }
    }

    [Route("api/app/attachment")]
    public class AttachmentController : AbpController
    {
        private readonly IFileAppService _fileAppService;
        private readonly IConfiguration _configuration;
        private readonly ISubmissionAppService _submissionAppService;
        private readonly IEmailLogAttachmentUploadService _emailLogAttachmentUploadService;
        private readonly ICurrentTenant _currentTenant;
        private readonly ILibreOfficeConversionService _libreOfficeConversionService;
        private readonly IAttachmentPreviewAppService _attachmentPreviewAppService;
        private ILogger Logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);
        private const string badRequestFileMsg = "File name must be provided.";
        private const string NotFoundFileMsg = "File not found.";
        private const string errorFileMsg = "An error occurred while downloading the file.";
        private const string chefsApiAccessError = "You do not have access to this resource";
        private const string fileProvidedError = "At least one file must be provided.";
        private const string libreOfficeNotInstalledMsg = "LibreOffice is not installed on the server. File preview is unavailable.";
        private const string ErrorCodeKey = "ErrorCode";
        private const string PdfContentType = "application/pdf";

        public AttachmentController(
            IFileAppService fileAppService,
            IConfiguration configuration,
            ISubmissionAppService submissionAppService,
            IEmailLogAttachmentUploadService emailLogAttachmentUploadService,
            ICurrentTenant currentTenant,
            ILibreOfficeConversionService libreOfficeConversionService,
            IAttachmentPreviewAppService attachmentPreviewAppService)
        {
            _fileAppService = fileAppService;
            _configuration = configuration;
            _submissionAppService = submissionAppService;
            _emailLogAttachmentUploadService = emailLogAttachmentUploadService;
            _currentTenant = currentTenant;
            _libreOfficeConversionService = libreOfficeConversionService;
            _attachmentPreviewAppService = attachmentPreviewAppService;
        }

        [HttpGet("applicant/{applicantId}/download/{fileName}")]
        public async Task<IActionResult> DownloadApplicantAttachment(string applicantId, string fileName)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(applicantId))
            {
                return BadRequest("Applicant ID must be provided.");
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return BadRequest(badRequestFileMsg);
            }

            var folder = _configuration["S3:ApplicantS3Folder"] ?? throw new AbpValidationException("Missing server configuration: S3:ApplicantS3Folder");

            if (!folder.EndsWith('/'))
            {
                folder += "/";
            }

            folder += applicantId;
            var key = folder + "/" + fileName;

            try
            {
                var fileDto = await _fileAppService.GetBlobAsync(new GetBlobRequestDto { S3ObjectKey = key, Name = fileName });

                if (fileDto == null || fileDto.Content == null)
                {
                    return NotFound(NotFoundFileMsg);
                }

                return File(fileDto.Content, fileDto.ContentType, fileDto.Name);
            }
            catch (Exception ex)
            {
                string ExceptionMessage = ex.Message;
                Logger.LogError(ex, "AttachmentController->DownloadApplicantAttachment: {ExceptionMessage}", ExceptionMessage);
                return StatusCode(500, errorFileMsg);
            }
        }

        [HttpGet("application/{applicationId}/download/{fileName}")]
        public async Task<IActionResult> DownloadApplicationAttachment(string applicationId, string fileName)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(applicationId))
            {
                return BadRequest("Application ID must be provided.");
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return BadRequest(badRequestFileMsg);
            }

            var folder = _configuration["S3:ApplicationS3Folder"] ?? throw new AbpValidationException("Missing server configuration: S3:ApplicationS3Folder");

            if (!folder.EndsWith('/'))
            {
                folder += "/";
            }

            folder += applicationId;
            var key = folder + "/" + fileName;

            try
            {
                var fileDto = await _fileAppService.GetBlobAsync(new GetBlobRequestDto { S3ObjectKey = key, Name = fileName });

                if (fileDto == null || fileDto.Content == null)
                {
                    return NotFound(NotFoundFileMsg);
                }

                return File(fileDto.Content, fileDto.ContentType, fileDto.Name);
            }
            catch (Exception ex)
            {
                string ExceptionMessage = ex.Message;
                Logger.LogError(ex, "AttachmentController->DownloadApplicationAttachment: {ExceptionMessage}", ExceptionMessage);
                return StatusCode(500, errorFileMsg);
            }
        }

        [HttpGet("assessment/{assessmentId}/download/{fileName}")]
        public async Task<IActionResult> DownloadAssessmentAttachment(string assessmentId, string fileName)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(assessmentId))
            {
                return BadRequest("Assessment ID must be provided.");
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return BadRequest(badRequestFileMsg);
            }

            var folder = _configuration["S3:AssessmentS3Folder"] ?? throw new AbpValidationException("Missing server configuration: S3:AssessmentS3Folder");

            if (!folder.EndsWith('/'))
            {
                folder += "/";
            }

            folder += assessmentId;
            var key = folder + "/" + fileName;

            try
            {
                var fileDto = await _fileAppService.GetBlobAsync(new GetBlobRequestDto { S3ObjectKey = key, Name = fileName });

                if (fileDto == null || fileDto.Content == null)
                {
                    return NotFound(NotFoundFileMsg);
                }

                return File(fileDto.Content, fileDto.ContentType, fileDto.Name);
            }
            catch (Exception ex)
            {
                string ExceptionMessage = ex.Message;
                Logger.LogError(ex, "AttachmentController->DownloadAssessmentAttachment: {ExceptionMessage}", ExceptionMessage);
                return StatusCode(500, errorFileMsg);
            }
        }

        [HttpGet("chefs/{formSubmissionId}/download/{chefsFileId}/{fileName}")]
        public async Task<IActionResult> DownloadChefsAttachment(Guid formSubmissionId, Guid chefsFileId, string fileName)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return BadRequest(badRequestFileMsg);
            }

            try
            {
                var fileDto = await _submissionAppService.GetChefsFileAttachment(formSubmissionId, chefsFileId, fileName);

                if (fileDto == null || fileDto.Content == null)
                {
                    return NotFound(NotFoundFileMsg);
                }

                return File(fileDto.Content, fileDto.ContentType, fileDto.Name);
            }
            catch (Exception ex)
            {
                string ExceptionMessage = ex.Message;
                Logger.LogError(ex, "AttachmentController->DownloadChefsAttachment: {ExceptionMessage}", ExceptionMessage);
                var errorCode = ex.Data.Contains(ErrorCodeKey) ? Convert.ToInt32(ex.Data[ErrorCodeKey]) : 500;
                return StatusCode(errorCode, ExceptionMessage);
            }
        }

        [HttpPost("chefs/download-all")]
        [Consumes("application/json")]
        public async Task<IActionResult> DownloadAllChefsAttachment([FromBody] List<AttachmentsDto> input)
        {
            var files = new List<FileContentResult>();

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            foreach (var item in input)
            {
                if (string.IsNullOrWhiteSpace(item.FileName))
                {
                    return BadRequest(badRequestFileMsg);
                }

                try
                {
                    var fileDto = await _submissionAppService.GetChefsFileAttachment(item.FormSubmissionId, item.ChefsFileId, item.FileName);
                    if (fileDto.Name == null || fileDto.Content == null)
                    {
                        return NotFound(NotFoundFileMsg);
                    }

                    byte[] fileBytes = fileDto.Content;
                    using (var ms = new MemoryStream(fileBytes))
                    {
                        files.Add(new FileContentResult(ms.ToArray(), "application/octet-stream")
                        {
                            FileDownloadName = fileDto.Name
                        });
                    }
                }
                catch (Exception ex)
                {
                    string ExceptionMessage = ex.Message;
                    Logger.LogError(ex, "AttachmentController->DownloadAllChefsAttachment: {ExceptionMessage}", ExceptionMessage);
                    if (ExceptionMessage.Contains(chefsApiAccessError))
                    {                        
                        return StatusCode(403, chefsApiAccessError);
                    }

                    return StatusCode(500, errorFileMsg);
                }
            }
            return Ok(files);
        }

        [HttpGet("application/{applicationId}/preview-pdf/{fileName}")]
        public async Task<IActionResult> PreviewApplicationAttachment(string applicationId, string fileName)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (string.IsNullOrWhiteSpace(applicationId)) return BadRequest("Application ID must be provided.");
            if (!Guid.TryParse(applicationId, out var parsedApplicationId)) return BadRequest("Application ID must be a valid GUID.");
            if (string.IsNullOrWhiteSpace(fileName)) return BadRequest(badRequestFileMsg);
            if (!LibreOfficeInstallationCache.IsInstalled(() => _libreOfficeConversionService.IsInstalled())) return StatusCode(503, new { error = libreOfficeNotInstalledMsg });
            try
            {
                var blob = await _attachmentPreviewAppService.GetOrCreatePreviewPdfAsync(AttachmentType.APPLICATION, parsedApplicationId, fileName);
                if (blob?.Content == null) return NotFound(NotFoundFileMsg);
                return File(blob.Content, PdfContentType);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "AttachmentController->PreviewApplicationAttachment: {Message}", ex.Message);
                return StatusCode(500, errorFileMsg);
            }
        }

        [HttpGet("assessment/{assessmentId}/preview-pdf/{fileName}")]
        public async Task<IActionResult> PreviewAssessmentAttachment(string assessmentId, string fileName)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (string.IsNullOrWhiteSpace(assessmentId)) return BadRequest("Assessment ID must be provided.");
            if (!Guid.TryParse(assessmentId, out var parsedAssessmentId)) return BadRequest("Assessment ID must be a valid GUID.");
            if (string.IsNullOrWhiteSpace(fileName)) return BadRequest(badRequestFileMsg);
            if (!LibreOfficeInstallationCache.IsInstalled(() => _libreOfficeConversionService.IsInstalled())) return StatusCode(503, new { error = libreOfficeNotInstalledMsg });
            try
            {
                var blob = await _attachmentPreviewAppService.GetOrCreatePreviewPdfAsync(AttachmentType.ASSESSMENT, parsedAssessmentId, fileName);
                if (blob?.Content == null) return NotFound(NotFoundFileMsg);
                return File(blob.Content, PdfContentType);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "AttachmentController->PreviewAssessmentAttachment: {Message}", ex.Message);
                return StatusCode(500, errorFileMsg);
            }
        }

        [HttpGet("applicant/{applicantId}/preview-pdf/{fileName}")]
        public async Task<IActionResult> PreviewApplicantAttachment(string applicantId, string fileName)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (string.IsNullOrWhiteSpace(applicantId)) return BadRequest("Applicant ID must be provided.");
            if (string.IsNullOrWhiteSpace(fileName)) return BadRequest(badRequestFileMsg);
            if (!_libreOfficeConversionService.IsInstalled()) return StatusCode(503, new { error = libreOfficeNotInstalledMsg });
            try
            {
                var blob = await _attachmentPreviewAppService.GetOrCreatePreviewPdfAsync(AttachmentType.APPLICANT, Guid.Parse(applicantId), fileName);
                if (blob?.Content == null) return NotFound(NotFoundFileMsg);
                return File(blob.Content, PdfContentType);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "AttachmentController->PreviewApplicantAttachment: {Message}", ex.Message);
                return StatusCode(500, errorFileMsg);
            }
        }

        [HttpGet("chefs/{formSubmissionId}/preview-pdf/{chefsFileId}/{fileName}")]
        public async Task<IActionResult> PreviewChefsAttachment(Guid formSubmissionId, Guid chefsFileId, string fileName)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (string.IsNullOrWhiteSpace(fileName)) return BadRequest(badRequestFileMsg);
            if (!_libreOfficeConversionService.IsInstalled()) return StatusCode(503, new { error = libreOfficeNotInstalledMsg });
            try
            {
                var chefsBlob = await _submissionAppService.GetChefsFileAttachment(formSubmissionId, chefsFileId, fileName);
                if (chefsBlob?.Content == null) return NotFound(NotFoundFileMsg);
                var blob = await _attachmentPreviewAppService.GetOrCreateChefsPreviewPdfAsync(formSubmissionId, chefsFileId, fileName, chefsBlob.Content);
                if (blob?.Content == null) return NotFound(NotFoundFileMsg);
                return File(blob.Content, PdfContentType);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "AttachmentController->PreviewChefsAttachment: {Message}", ex.Message);
                var errorCode = ex.Data.Contains(ErrorCodeKey) ? Convert.ToInt32(ex.Data[ErrorCodeKey]) : 500;
                return StatusCode(errorCode, errorFileMsg);
            }
        }

        [HttpPost("applicant/{applicantId}/upload")]
#pragma warning disable IDE0060 // Remove unused parameter
        public async Task<IActionResult> UploadApplicantAttachments(Guid applicantId, IList<IFormFile> files, string userId, string userName)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (files == null || files.Count == 0)
            {
                return BadRequest(fileProvidedError);
            }

            return await UploadFiles(files);
        }

        [HttpPost("assessment/{assessmentId}/upload")]
#pragma warning disable IDE0060 // Remove unused parameter
        public async Task<IActionResult> UploadAssessmentAttachments(Guid assessmentId, IList<IFormFile> files)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (files == null || files.Count == 0)
            {
                return BadRequest(fileProvidedError);
            }

            return await UploadFiles(files);
        }

        [HttpPost("application/{applicationId}/upload")]
#pragma warning disable IDE0060 // Remove unused parameter
        public async Task<IActionResult> UploadApplicationAttachments(Guid applicationId, IList<IFormFile> files, string userId, string userName)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (files == null || files.Count == 0)
            {
                return BadRequest(fileProvidedError);
            }

            return await UploadFiles(files);
        }

        [HttpPost("template/{templateId}/upload")]
        public async Task<IActionResult> UploadTemplateAttachments(Guid templateId, IList<IFormFile> files)
        {
            return await UploadEmailAttachments(null, templateId, files);
        }

        [HttpPost("email/{emailLogId}/upload")]
        public async Task<IActionResult> UploadEmailAttachments(Guid emailLogId, IList<IFormFile> files)
        {
            return await UploadEmailAttachments(emailLogId, null, files);
        }

        [NonAction]
        public async Task<IActionResult> UploadEmailAttachments(Guid? emailLogId, Guid? templateId, IList<IFormFile> files)
        { 
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (files == null || files.Count == 0)
            {
                return BadRequest(fileProvidedError);
            }

            List<ValidationResult> invalidMetadata = GetInvalidFileMetadata(files);
            if (invalidMetadata.Count > 0)
            {
                throw new AbpValidationException(message: "ERROR: Invalid File Type.", validationErrors: invalidMetadata);
            }

            // Email-specific size checks are metadata-only (file.Length / a total-size lookup),
            // so they run before any file is buffered into memory, same as GetInvalidFileMetadata.
            // A missing OR malformed config value both fall back to the default and the check
            // still runs - a parse failure must not silently disable enforcement.
            var emailAttachmentMaxFileSizeConfig = _configuration["S3:EmailAttachmentMaxFileSize"] ?? "20";
            if (!double.TryParse(emailAttachmentMaxFileSizeConfig, out double maxFileSizeMB) || maxFileSizeMB <= 0)
            {
                maxFileSizeMB = 20;
            }

            var oversizedFiles = files.Where(f => f.Length * 0.000001 > maxFileSizeMB).ToList();
            if (oversizedFiles.Count > 0)
            {
                var sizeErrors = oversizedFiles.Select(f =>
                    new ValidationResult($"File '{f.FileName}' exceeds the maximum allowed size of {maxFileSizeMB} MB for email attachments.", [f.FileName])
                ).ToList();
                throw new AbpValidationException("One or more files exceed the maximum allowed size for email attachments.", sizeErrors);
            }

            var totalMaxFileSizeConfig = _configuration["S3:EmailAttachmentsTotalMaxFileSize"] ?? "25";
            if (!double.TryParse(totalMaxFileSizeConfig, out double totalMaxSizeMB) || totalMaxSizeMB <= 0)
            {
                totalMaxSizeMB = 25;
            }

            long existingTotalBytes = await _emailLogAttachmentUploadService
                .GetTotalFileSizeByEmailLogIdAsync(emailLogId, templateId);
            long newFilesBytes = files.Sum(f => f.Length);
            double combinedMB = (existingTotalBytes + newFilesBytes) * 0.000001;

            if (combinedMB > totalMaxSizeMB)
            {
                throw new AbpValidationException(
                    $"The total size of all attachments ({combinedMB:F1} MB) would exceed the maximum allowed {totalMaxSizeMB} MB for email attachments. Please remove existing attachments or select a smaller file.",
                    [new ValidationResult("Total attachment size exceeds the allowed limit.")]);
            }

            List<(IFormFile File, byte[] Content)> fileEntries = await ReadFilesAsync(files);
            List<ValidationResult> invalidContent = GetInvalidFileContent(fileEntries);
            if (invalidContent.Count > 0)
            {
                throw new AbpValidationException(message: "ERROR: Invalid File Type.", validationErrors: invalidContent);
            }

            var results = new List<object>();
            foreach (var (file, content) in fileEntries)
            {
                try
                {
                    var dto = await _emailLogAttachmentUploadService.UploadAsync(
                        emailLogId,
                        templateId,
                        _currentTenant.Id,
                        file.FileName,
                        content,
                        file.ContentType ?? "application/octet-stream");
                    results.Add(dto);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "AttachmentController->UploadEmailAttachments: Failed to upload {FileName}", file.FileName);
                    return StatusCode(500, $"Failed to upload {file.FileName}: {ex.Message}");
                }
            }

            return Ok(results);
        }

        private async Task<IActionResult> UploadFiles(IList<IFormFile> files)
        {
            List<ValidationResult> invalidMetadata = GetInvalidFileMetadata(files);
            if (invalidMetadata.Count > 0)
            {
                throw new AbpValidationException(message: "ERROR: Invalid File Type.", validationErrors: invalidMetadata);
            }

            List<(IFormFile File, byte[] Content)> fileEntries = await ReadFilesAsync(files);
            List<ValidationResult> invalidContent = GetInvalidFileContent(fileEntries);
            if (invalidContent.Count > 0)
            {
                throw new AbpValidationException(message: "ERROR: Invalid File Type.", validationErrors: invalidContent);
            }
            List<string> ErrorList = [];
            foreach (var (source, content) in fileEntries)
            {
                try
                {
                    await _fileAppService.SaveBlobAsync(
                        new SaveBlobInputDto
                        {
                            Name = source.FileName,
                            Content = content
                        });
                }
                catch (Exception ex)
                {
                    ErrorList.Add(source.FileName + " : " + ex.Message);
                }

            }

            if (ErrorList.Count > 0)
            {
                return BadRequest("ERROR:<br>" + String.Join("<br>", ErrorList.ToArray()));
            }

            return Ok("All Files Are Successfully Uploaded!");
        }

        private static async Task<List<(IFormFile File, byte[] Content)>> ReadFilesAsync(IList<IFormFile> files)
        {
            var fileEntries = new List<(IFormFile File, byte[] Content)>();
            foreach (var file in files)
            {
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                fileEntries.Add((file, memoryStream.ToArray()));
            }
            return fileEntries;
        }

        // Extensions for which the browser-supplied ContentType and file signature (magic bytes)
        // are reliable enough to validate; plain text formats (txt/csv) have no consistent signature
        // or ContentType across browsers/OSes, so only the allowlist and size checks apply to them.
        private static readonly HashSet<string> StrictlyValidatedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            "pdf", "doc", "docx", "xls", "xlsx", "ppt", "pptx", "jpg", "jpeg", "png", "gif", "zip"
        };

        private static string GetExtension(string fileName)
        {
            var extension = Path.GetExtension(fileName);
            if (extension.StartsWith('.'))
            {
                extension = extension[1..];
            }
            return extension.ToLowerInvariant();
        }

        // Used when S3:AllowedFileTypes is missing or fails to parse, so a config gap degrades
        // to this known-safe, already-reviewed set rather than silently rejecting every upload
        // (fail-closed-to-nothing) or silently allowing anything (fail-open).
        private static readonly string[] DefaultAllowedFileTypes =
        [
            "pdf", "doc", "docx", "xls", "xlsx", "ppt", "pptx", "jpg", "jpeg", "png", "gif", "txt", "csv", "zip"
        ];

        private string[]? GetConfiguredFileTypesOrNull()
        {
            var allowedFileTypesConfig = _configuration["S3:AllowedFileTypes"];
            if (string.IsNullOrWhiteSpace(allowedFileTypesConfig))
            {
                return null;
            }

            try
            {
                var allowedFileTypes = JsonConvert.DeserializeObject<string[]>(allowedFileTypesConfig);
                if (allowedFileTypes == null || allowedFileTypes.Length == 0)
                {
                    Logger.LogWarning("AttachmentController: S3:AllowedFileTypes was empty; falling back to the default allowlist.");
                    return null;
                }
                return allowedFileTypes;
            }
            catch (JsonException ex)
            {
                Logger.LogWarning(ex, "AttachmentController: S3:AllowedFileTypes could not be parsed as a JSON string array; falling back to the default allowlist.");
                return null;
            }
        }

        // S3:AllowedFileTypes can only ever narrow this controller's effective allowlist, never
        // broaden it. DefaultAllowedFileTypes is exactly the set of extensions this controller
        // knows how to validate at the content level (StrictlyValidatedExtensions' signature and
        // content-type checks, plus txt/csv which are deliberately exempt since they're inert
        // text with no reliable signature). Without this ceiling, an operator accidentally (or
        // maliciously) adding e.g. "exe" to config would let it pass the extension check while
        // still being skipped by the content-level checks - reintroducing CWE-434 via config
        // alone, with no code change required.
        private string[] GetAllowedFileTypes()
        {
            var configuredFileTypes = GetConfiguredFileTypesOrNull();
            if (configuredFileTypes == null)
            {
                return DefaultAllowedFileTypes;
            }

            var normalizedConfiguredTypes = configuredFileTypes.Select(t => t.ToLowerInvariant()).ToArray();
            var effectiveFileTypes = normalizedConfiguredTypes
                .Where(t => DefaultAllowedFileTypes.Contains(t))
                .Distinct()
                .ToArray();

            var rejectedFileTypes = normalizedConfiguredTypes.Except(effectiveFileTypes).ToList();
            if (rejectedFileTypes.Count > 0)
            {
                Logger.LogWarning(
                    "AttachmentController: S3:AllowedFileTypes configured extension(s) [{RejectedFileTypes}] are outside the validated safe set and were ignored.",
                    string.Join(", ", rejectedFileTypes));
            }

            return effectiveFileTypes.Length > 0 ? effectiveFileTypes : DefaultAllowedFileTypes;
        }

        // Cheap, metadata-only checks (extension + declared size) that require no stream I/O.
        // Run these before ever reading a file's bytes, so an oversized or disallowed file is
        // rejected without being buffered into memory or scanned for a signature match.
        private List<ValidationResult> GetInvalidFileMetadata(IList<IFormFile> files)
        {
            List<ValidationResult> ErrorList = [];
            var AllowedFileTypes = GetAllowedFileTypes();

            var maxFileSizeConfig = _configuration["S3:MaxFileSize"] ?? "25";
            if (!double.TryParse(maxFileSizeConfig, out double maxFileSizeMB) || maxFileSizeMB <= 0)
            {
                maxFileSizeMB = 25;
            }

            foreach (var file in files)
            {
                var fileName = file.FileName;
                var extension = GetExtension(fileName);

                if (!AllowedFileTypes.Contains(extension))
                {
                    ErrorList.Add(new ValidationResult("Invalid file type for " + fileName, [nameof(fileName)]));
                    continue;
                }

                if (file.Length * 0.000001 > maxFileSizeMB)
                {
                    ErrorList.Add(new ValidationResult($"File '{fileName}' exceeds the maximum allowed size of {maxFileSizeMB} MB.", [nameof(fileName)]));
                }
            }
            return ErrorList;
        }

        // Content-level checks (content-type consistency + magic-byte signature) that require
        // the file's bytes. Only called for files that already passed GetInvalidFileMetadata.
        private static List<ValidationResult> GetInvalidFileContent(List<(IFormFile File, byte[] Content)> fileEntries)
        {
            List<ValidationResult> ErrorList = [];
            var contentTypeProvider = new FileExtensionContentTypeProvider();

            foreach (var (file, content) in fileEntries)
            {
                var fileName = file.FileName;
                var extension = GetExtension(fileName);

                if (!StrictlyValidatedExtensions.Contains(extension))
                {
                    continue;
                }

                if (contentTypeProvider.TryGetContentType(fileName, out var expectedContentType) &&
                    !string.IsNullOrWhiteSpace(file.ContentType) &&
                    !string.Equals(file.ContentType, "application/octet-stream", StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(expectedContentType.Split('/')[0], file.ContentType.Split('/')[0], StringComparison.OrdinalIgnoreCase))
                {
                    ErrorList.Add(new ValidationResult("File content type does not match its extension for " + fileName, [nameof(fileName)]));
                    continue;
                }

                if (!FileSignatureValidator.HasValidSignature(extension, content))
                {
                    ErrorList.Add(new ValidationResult("File content does not match its expected format for " + fileName, [nameof(fileName)]));
                }
            }
            return ErrorList;
        }
    }
}
