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
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Unity.GrantManager.Models;

namespace Unity.GrantManager.Controllers
{
    [Route("api/app/attachment")]
    public class AttachmentController : AbpController
    {
        private readonly IFileAppService _fileAppService;
        private readonly IConfiguration _configuration;
        private readonly ISubmissionAppService _submissionAppService;
        private ILogger logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);
        private const string badRequestFileMsg = "File name must be provided.";
        private const string NotFoundFileMsg = "File not found.";
        private const string errorFileMsg = "An error occurred while downloading the file.";
        private const string chefsApiAccessError = "You do not have access to this resource";

        public AttachmentController(IFileAppService fileAppService, IConfiguration configuration, ISubmissionAppService submissionAppService)
        {
            _fileAppService = fileAppService;
            _configuration = configuration;
            _submissionAppService = submissionAppService;
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
                return BadRequest("At least one file must be provided.");
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
                return BadRequest("At least one file must be provided.");
            }

            return await UploadFiles(files);
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
