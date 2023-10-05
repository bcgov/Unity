using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Unity.GrantManager.Attachments;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Validation;

namespace Unity.GrantManager.Controllers
{
    public class AttachmentController : AbpController
    {
        private readonly IFileAppService _fileAppService;
        private readonly IConfiguration _configuration;

        public AttachmentController(IFileAppService fileAppService, IConfiguration configuration)
        {
            _fileAppService = fileAppService;
            _configuration = configuration;
        }

        [HttpGet]
        [Route("/api/app/attachment/application/{applicationId}/download/{fileName}")]
        public async Task<IActionResult> DownloadApplicationAttachment(string applicationId, string fileName)
        {
            var folder = _configuration["S3:ApplicationS3Folder"] ?? throw new AbpValidationException("Missing server configuration: S3:ApplicationS3Folder");
            if (!folder.EndsWith('/'))
            {
                folder += "/";
            }
            folder += applicationId;
            var key = folder + "/" + fileName;
            var fileDto = await _fileAppService.GetBlobAsync(new GetBlobRequestDto { S3ObjectKey = key, Name = fileName }); 
            return File(fileDto.Content, fileDto.ContentType, fileDto.Name);
        }

        [HttpGet]
        [Route("/api/app/attachment/assessment/{assessmentId}/download/{fileName}")]
        public async Task<IActionResult> DownloadAssessmentAttachment(string assessmentId, string fileName)
        {
            var folder = _configuration["S3:AssessmentS3Folder"] ?? throw new AbpValidationException("Missing server configuration: S3:AssessmentS3Folder");
            if (!folder.EndsWith('/'))
            {
                folder += "/";
            }
            folder += assessmentId;
            var key = folder + "/" + fileName;
            var fileDto = await _fileAppService.GetBlobAsync(new GetBlobRequestDto { S3ObjectKey = key, Name = fileName });
            return File(fileDto.Content, fileDto.ContentType, fileDto.Name);
        }

        [HttpPost]
        [Route("/api/app/attachment/assessment/{assessmentId}/upload")]
#pragma warning disable IDE0060 // Remove unused parameter
        public async Task<IActionResult> UploadAssessmentAttachments(Guid assessmentId, IList<IFormFile> files, string userId, string userName)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            return await UploadFiles(files);            
        }

        [HttpPost]
        [Route("/api/app/attachment/application/{applicationId}/upload")]
#pragma warning disable IDE0060 // Remove unused parameter
        public async Task<IActionResult> UploadApplicationAttachments(Guid applicationId, IList<IFormFile> files, string userId, string userName)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            return await UploadFiles(files);
        }

        private async Task<IActionResult> UploadFiles(IList<IFormFile> files)
        {
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
    }
}
