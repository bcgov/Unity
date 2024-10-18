using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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

        protected ILogger logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);
 
        [HttpGet("export/{worksheetId}")]
        public async Task<IActionResult> ExportWorksheet(Guid worksheetId)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid request.");
            }

            try
            {
                var fileDto = await _worksheetAppService.ExportWorksheet(worksheetId);

                if (fileDto == null)
                {
                    return NotFound("Worksheet not found.");
                }

                if (string.IsNullOrEmpty(fileDto.ContentType))
                {
                    return BadRequest("Invalid content type.");
                }

                return File(fileDto.Content, fileDto.ContentType, fileDto.Name);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error exporting worksheet with ID: {WorksheetId}", worksheetId);
                return StatusCode(500, "An error occurred while exporting the worksheet.");
            }
        }

        [HttpPost("import")]
        public async Task<IActionResult> ImportWorksheetFile(IFormFile file)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("ImportWorksheetFile: Invalid model state.");
            }

            if (file == null || file.Length == 0 || !file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest("ImportWorksheetFile: Invalid file. Please upload a valid JSON file.");
            }

            try
            {
                var jsonString = await ReadJsonFileAsync(file);
                await _worksheetAppService.ImportWorksheetAsync(new WorksheetImportDto
                {
                    Name = file.FileName,
                    Content = jsonString
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error importing worksheet file: {FileName}", file.FileName);
                return BadRequest($"ERROR:<br>{file.FileName} : {ex.Message}");
            }

            return Ok("Worksheet is successfully imported!");
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
