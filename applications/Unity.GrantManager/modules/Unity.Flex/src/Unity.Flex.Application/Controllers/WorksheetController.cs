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
using Unity.Flex.Worksheets;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Validation;

namespace Unity.Flex.Controllers
{
    public class WorksheetController : AbpController
    {

        private readonly IWorksheetAppService _worksheetAppService;

        public WorksheetController(IWorksheetAppService worksheetAppService)
        {
            _worksheetAppService = worksheetAppService;
        }
        

        [HttpGet]
        [Route("/api/app/worksheet/export/{worksheetId}")]
        public async Task<IActionResult> ExportWorksheet(Guid worksheetId)
        {
            var fileDto = await _worksheetAppService.ExportWorksheet(worksheetId);
            return File(fileDto.Content, fileDto.ContentType, fileDto.Name);
        }

        
    }
}
