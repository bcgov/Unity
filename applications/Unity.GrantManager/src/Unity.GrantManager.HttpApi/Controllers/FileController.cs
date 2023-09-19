using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Unity.GrantManager.Attachments;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Controllers
{
    public class FileController : AbpController
    {
        private readonly IFileAppService _fileAppService;

        public FileController(IFileAppService fileAppService)
        {
            _fileAppService = fileAppService;
        }

        [HttpGet]
        [Route("download")]
        public async Task<IActionResult> DownloadAsync(string s3Guid, string name)
        {
            var fileDto = await _fileAppService.GetBlobAsync(new GetBlobRequestDto { S3Guid = new Guid(s3Guid), Name = name }); 
            return File(fileDto.Content, fileDto.ContentType, fileDto.Name);
        }

        [HttpPost]
        [Route("uploader")]
        public async Task<IActionResult> Index(IList<IFormFile> files)
        {
            List<string> ErrorList = new();
            foreach (IFormFile source in files)
            {
                try
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await source.CopyToAsync(memoryStream);
                        await _fileAppService.SaveBlobAsync(
                            new SaveBlobInputDto
                            {
                                Name = source.FileName,
                                Content = memoryStream.ToArray()
                            });
                    }
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
