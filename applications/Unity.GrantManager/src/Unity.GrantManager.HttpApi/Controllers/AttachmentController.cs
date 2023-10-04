using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Unity.GrantManager.Attachments;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Controllers
{
    public class AttachmentController : AbpController
    {
        private readonly IFileAppService _fileAppService;

        public AttachmentController(IFileAppService fileAppService)
        {
            _fileAppService = fileAppService;
        }

        [HttpGet]
        [Route("download")]
        public async Task<IActionResult> DownloadAsync(string s3ObjectKey, string name)
        {
            var fileDto = await _fileAppService.GetBlobAsync(new GetBlobRequestDto { S3ObjectKey = s3ObjectKey, Name = name }); 
            return File(fileDto.Content, fileDto.ContentType, fileDto.Name);
        }

        [HttpPost]
        [Route("/api/app/attachment/assessment/{assessmentId}/upload")]
        public async Task<IActionResult> UploadAssessmentAttachments(Guid assessmentId, IList<IFormFile> files, string userId, string userName)
        {
            return await UploadFiles(files);            
        }

        [HttpPost]
        [Route("/api/app/attachment/application/{applicationId}/upload")]
        public async Task<IActionResult> UploadApplicationAttachments(Guid applicationId, IList<IFormFile> files, string userId, string userName)
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
