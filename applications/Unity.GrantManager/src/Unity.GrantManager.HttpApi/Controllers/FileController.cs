using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
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
        [Route("download/{S3Guid}")]
        public async Task<IActionResult> DownloadAsync(string s3Guid)
        {
            var fileDto = await _fileAppService.GetBlobAsync(new GetBlobRequestDto { S3Guid = new Guid(s3Guid) });
            return File(fileDto.Content, "application/octet-stream", fileDto.Name);
        }

        [HttpPost]
        [Route("uploader")]
        public async Task<IActionResult> Index(IList<IFormFile> files)
        {
            List<string> ErrorList = new List<string>();
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

            if(ErrorList.Count > 0)
            {
                return BadRequest("ERROR:<br>" + String.Join("<br>", ErrorList.ToArray()));
            }

            return Ok("All Files Are Successfully Uploaded!");          

            
        }
    }

    public class UploadFileDto
    {
        [Required]
        [Display(Name = "File")]
        public IFormFile File { get; set; }

    }
}
