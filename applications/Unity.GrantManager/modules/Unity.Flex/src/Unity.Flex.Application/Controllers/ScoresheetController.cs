using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
        protected ILogger logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);
        
        [HttpGet("export/{scoresheetId}")]
        public async Task<IActionResult> ExportScoresheet(Guid scoresheetId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid model state.");
            }

            try
            {
                var fileDto = await scoresheetAppService.ExportScoresheet(scoresheetId);
                
                if (fileDto == null)
                {
                    return NotFound("Scoresheet not found.");
                }

                if (string.IsNullOrEmpty(fileDto.ContentType))
                {
                    return BadRequest("Invalid content type.");
                }

                return File(fileDto.Content, fileDto.ContentType, fileDto.Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error exporting scoresheet with ID: {ScoresheetId}", scoresheetId);
                return StatusCode(500, "An error occurred while exporting the scoresheet.");
            }
        }

        [HttpPost("import")]
        public async Task<IActionResult> ImportScoresheetFile(IFormFile file)
        {
            if (!ModelState.IsValid || file == null || file.Length == 0 || !file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("Invalid file. Please upload a valid JSON file.");
            }

            try
            {
                var jsonString = await ReadJsonFileAsync(file);
                await scoresheetAppService.ImportScoresheetAsync(new ScoresheetImportDto
                {
                    Name = file.FileName,
                    Content = jsonString
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error importing scoresheet file: {FileName}", file.FileName);
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
