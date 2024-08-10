using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;
using Unity.Flex.Scoresheets;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex.Controllers
{
    [Route("/api/app/scoresheet")]
    public class ScoresheetController(IScoresheetAppService scoresheetAppService) : AbpController
    {

        [HttpGet("export/{scoresheetId}")]
        public async Task<IActionResult> ExportScoresheet(Guid scoresheetId)
        {
            var fileDto = await scoresheetAppService.ExportScoresheet(scoresheetId);
            return File(fileDto.Content, fileDto.ContentType, fileDto.Name);
        }

        [HttpPost("import")]
        public async Task<IActionResult> ImportScoresheetFile(IFormFile file)
        {
            try
            {
                var jsonString = await ReadJsonFileAsync(file);
                await scoresheetAppService.ImportScoresheetAsync(
                    new ScoresheetImportDto
                    {
                        Name = file.FileName,
                        Content = jsonString
                    });
            }
            catch (Exception ex)
            {
                return BadRequest("ERROR:<br>" + file.FileName + " : " + ex.Message);
            }

            return Ok("Scoresheet Is Successfully Imported!");
        }

        
        private static async Task<string> ReadJsonFileAsync(IFormFile jsonFile)
        {
            if (jsonFile == null || jsonFile.Length == 0)
            {
                throw new ArgumentException("No file provided or file is empty.");
            }

            using var stream = jsonFile.OpenReadStream();
            using var reader = new StreamReader(stream);
            return await reader.ReadToEndAsync();
        }
    }
}
