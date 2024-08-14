using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;
using Unity.Flex.Worksheets;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex.Controllers
{
    [Route("/api/app/worksheet")]
    public class WorksheetController : AbpController
    {

        private readonly IWorksheetAppService _worksheetAppService;

        public WorksheetController(IWorksheetAppService worksheetAppService)
        {
            _worksheetAppService = worksheetAppService;
        }


        [HttpGet("export/{worksheetId}")]
        public async Task<IActionResult> ExportWorksheet(Guid worksheetId)
        {
            var fileDto = await _worksheetAppService.ExportWorksheet(worksheetId);
            return File(fileDto.Content, fileDto.ContentType, fileDto.Name);
        }

        [HttpPost("import")]
        public async Task<IActionResult> ImportWorksheetFile(IFormFile file)
        {
            try
            {
                var jsonString = await ReadJsonFileAsync(file);
                await _worksheetAppService.ImportWorksheetAsync(
                    new WorksheetImportDto
                    {
                        Name = file.FileName,
                        Content = jsonString
                    });
            }
            catch (Exception ex)
            {
                return BadRequest("ERROR:<br>" + file.FileName + " : " + ex.Message);
            }

            return Ok("Worksheet Is Successfully Imported!");
        }

        private static async Task<string> ReadJsonFileAsync(IFormFile jsonFile)
        {
            if (jsonFile == null || jsonFile.Length == 0)
            {
                throw new ArgumentException("No file provided or file is empty.");
            }

            using (var stream = jsonFile.OpenReadStream())
            using (var reader = new StreamReader(stream))
            {
                return await reader.ReadToEndAsync();
            }
        }
    }
}
