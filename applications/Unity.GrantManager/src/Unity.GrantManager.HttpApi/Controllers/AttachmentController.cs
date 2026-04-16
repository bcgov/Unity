using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Unity.GrantManager.Attachments;
using Unity.GrantManager.Intakes;
using Unity.Notifications.Emails;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Unity.GrantManager.Models;
using System.Threading;

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

namespace Unity.GrantManager.Controllers
{
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
        private ILogger logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);
        private const string badRequestFileMsg = "File name must be provided.";
        private const string NotFoundFileMsg = "File not found.";
        private const string errorFileMsg = "An error occurred while downloading the file.";
        private const string chefsApiAccessError = "You do not have access to this resource";
        private const string fileProvidedError = "At least one file must be provided.";
        private const string libreOfficeNotInstalledMsg = "LibreOffice is not installed on the server. File preview is unavailable.";

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
                logger.LogError(ex, "AttachmentController->DownloadApplicantAttachment: {ExceptionMessage}", ExceptionMessage);
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
                logger.LogError(ex, "AttachmentController->DownloadApplicationAttachment: {ExceptionMessage}", ExceptionMessage);
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
                logger.LogError(ex, "AttachmentController->DownloadAssessmentAttachment: {ExceptionMessage}", ExceptionMessage);
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
                logger.LogError(ex, "AttachmentController->DownloadChefsAttachment: {ExceptionMessage}", ExceptionMessage);
                var errorCode = ex.Data.Contains("ErrorCode") ? Convert.ToInt32(ex.Data["ErrorCode"]) : 500;
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
                    logger.LogError(ex, "AttachmentController->DownloadAllChefsAttachment: {ExceptionMessage}", ExceptionMessage);
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
            if (string.IsNullOrWhiteSpace(applicationId)) return BadRequest("Application ID must be provided.");
            if (string.IsNullOrWhiteSpace(fileName)) return BadRequest(badRequestFileMsg);
            if (!LibreOfficeInstallationCache.IsInstalled(() => _libreOfficeConversionService.IsInstalled())) return StatusCode(503, new { error = libreOfficeNotInstalledMsg });
            try
            {
                var blob = await _attachmentPreviewAppService.GetOrCreatePreviewPdfAsync(AttachmentType.APPLICATION, Guid.Parse(applicationId), fileName);
                if (blob?.Content == null) return NotFound(NotFoundFileMsg);
                return File(blob.Content, "application/pdf");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AttachmentController->PreviewApplicationAttachment: {Message}", ex.Message);
                return StatusCode(500, errorFileMsg);
            }
        }

        [HttpGet("assessment/{assessmentId}/preview-pdf/{fileName}")]
        public async Task<IActionResult> PreviewAssessmentAttachment(string assessmentId, string fileName)
        {
            if (string.IsNullOrWhiteSpace(assessmentId)) return BadRequest("Assessment ID must be provided.");
            if (string.IsNullOrWhiteSpace(fileName)) return BadRequest(badRequestFileMsg);
            if (!LibreOfficeInstallationCache.IsInstalled(() => _libreOfficeConversionService.IsInstalled())) return StatusCode(503, new { error = libreOfficeNotInstalledMsg });
            try
            {
                var blob = await _attachmentPreviewAppService.GetOrCreatePreviewPdfAsync(AttachmentType.ASSESSMENT, Guid.Parse(assessmentId), fileName);
                if (blob?.Content == null) return NotFound(NotFoundFileMsg);
                return File(blob.Content, "application/pdf");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AttachmentController->PreviewAssessmentAttachment: {Message}", ex.Message);
                return StatusCode(500, errorFileMsg);
            }
        }

        [HttpGet("applicant/{applicantId}/preview-pdf/{fileName}")]
        public async Task<IActionResult> PreviewApplicantAttachment(string applicantId, string fileName)
        {
            if (string.IsNullOrWhiteSpace(applicantId)) return BadRequest("Applicant ID must be provided.");
            if (string.IsNullOrWhiteSpace(fileName)) return BadRequest(badRequestFileMsg);
            if (!_libreOfficeConversionService.IsInstalled()) return StatusCode(503, new { error = libreOfficeNotInstalledMsg });
            try
            {
                var blob = await _attachmentPreviewAppService.GetOrCreatePreviewPdfAsync(AttachmentType.APPLICANT, Guid.Parse(applicantId), fileName);
                if (blob?.Content == null) return NotFound(NotFoundFileMsg);
                return File(blob.Content, "application/pdf");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AttachmentController->PreviewApplicantAttachment: {Message}", ex.Message);
                return StatusCode(500, errorFileMsg);
            }
        }

        [HttpGet("chefs/{formSubmissionId}/preview-pdf/{chefsFileId}/{fileName}")]
        public async Task<IActionResult> PreviewChefsAttachment(Guid formSubmissionId, Guid chefsFileId, string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return BadRequest(badRequestFileMsg);
            if (!_libreOfficeConversionService.IsInstalled()) return StatusCode(503, new { error = libreOfficeNotInstalledMsg });
            try
            {
                var chefsBlob = await _submissionAppService.GetChefsFileAttachment(formSubmissionId, chefsFileId, fileName);
                if (chefsBlob?.Content == null) return NotFound(NotFoundFileMsg);
                var blob = await _attachmentPreviewAppService.GetOrCreateChefsPreviewPdfAsync(formSubmissionId, chefsFileId, fileName, chefsBlob.Content);
                if (blob?.Content == null) return NotFound(NotFoundFileMsg);
                return File(blob.Content, "application/pdf");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AttachmentController->PreviewChefsAttachment: {Message}", ex.Message);
                var errorCode = ex.Data.Contains("ErrorCode") ? Convert.ToInt32(ex.Data["ErrorCode"]) : 500;
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

        [HttpPost("email/{emailLogId}/upload")]
        public async Task<IActionResult> UploadEmailAttachments(Guid emailLogId, IList<IFormFile> files)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (files == null || files.Count == 0)
            {
                return BadRequest(fileProvidedError);
            }

            List<ValidationResult> invalidFileTypes = GetInvalidFileTypes(files);
            if (invalidFileTypes.Count > 0)
            {
                throw new AbpValidationException(message: "ERROR: Invalid File Type.", validationErrors: invalidFileTypes);
            }

            var emailAttachmentMaxFileSizeConfig = _configuration["S3:EmailAttachmentMaxFileSize"] ?? "20";
            if (double.TryParse(emailAttachmentMaxFileSizeConfig, out double maxFileSizeMB))
            {
                var oversizedFiles = files.Where(f => f.Length * 0.000001 > maxFileSizeMB).ToList();
                if (oversizedFiles.Count > 0)
                {
                    var sizeErrors = oversizedFiles.Select(f =>
                        new ValidationResult($"File '{f.FileName}' exceeds the maximum allowed size of {maxFileSizeMB} MB for email attachments.", [f.FileName])
                    ).ToList();
                    throw new AbpValidationException("One or more files exceed the maximum allowed size for email attachments.", sizeErrors);
                }
            }

            var totalMaxFileSizeConfig = _configuration["S3:EmailAttachmentsTotalMaxFileSize"] ?? "25";
            if (double.TryParse(totalMaxFileSizeConfig, out double totalMaxSizeMB))
            {
                long existingTotalBytes = await _emailLogAttachmentUploadService
                    .GetTotalFileSizeByEmailLogIdAsync(emailLogId);
                long newFilesBytes = files.Sum(f => f.Length);
                double combinedMB = (existingTotalBytes + newFilesBytes) * 0.000001;

                if (combinedMB > totalMaxSizeMB)
                {
                    throw new AbpValidationException(
                        $"The total size of all attachments ({combinedMB:F1} MB) would exceed the maximum allowed {totalMaxSizeMB} MB for email attachments. Please remove existing attachments or select a smaller file.",
                        [new ValidationResult("Total attachment size exceeds the allowed limit.")]);
                }
            }

            var results = new List<object>();
            foreach (var file in files)
            {
                try
                {
                    using var ms = new MemoryStream();
                    await file.CopyToAsync(ms);
                    var dto = await _emailLogAttachmentUploadService.UploadAsync(
                        emailLogId,
                        _currentTenant.Id,
                        file.FileName,
                        ms.ToArray(),
                        file.ContentType ?? "application/octet-stream");
                    results.Add(dto);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "AttachmentController->UploadEmailAttachments: Failed to upload {FileName}", file.FileName);
                    return StatusCode(500, $"Failed to upload {file.FileName}: {ex.Message}");
                }
            }

            return Ok(results);
        }

        private async Task<IActionResult> UploadFiles(IList<IFormFile> files)
        {
            List<ValidationResult> InvalidFileTypes = GetInvalidFileTypes(files);
            if (InvalidFileTypes.Count > 0)
            {
                throw new AbpValidationException(message: "ERROR: Invalid File Type.", validationErrors: InvalidFileTypes);
            }
            List<string> ErrorList = new();
            foreach (IFormFile source in files)
            {
                try
                {
                    using var memoryStream = new MemoryStream();
                    await source.CopyToAsync(memoryStream);
                    await _fileAppService.SaveBlobAsync(
                        new SaveBlobInputDto
                        {
                            Name = source.FileName,
                            Content = memoryStream.ToArray()
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

        private List<ValidationResult> GetInvalidFileTypes(IList<IFormFile> files)
        {
            List<ValidationResult> ErrorList = new();
            var InvalidFileTypes = _configuration["S3:DisallowedFileTypes"] ?? "";
            var DisallowedFileTypes = JsonConvert.DeserializeObject<string[]>(InvalidFileTypes);
            if (DisallowedFileTypes == null)
            {
                return ErrorList;
            }
            foreach (var fileName in files.Where(file =>
            {
                string FileType = Path.GetExtension(file.FileName);
                if (FileType.StartsWith('.'))
                {
                    FileType = FileType[1..];
                }
                return DisallowedFileTypes.Contains(FileType.ToLower());
            }).Select(source => source.FileName))
            {
                ErrorList.Add(new ValidationResult("Invalid file type for " + fileName, [nameof(fileName)]));
            }
            return ErrorList;
        }
    }
}
