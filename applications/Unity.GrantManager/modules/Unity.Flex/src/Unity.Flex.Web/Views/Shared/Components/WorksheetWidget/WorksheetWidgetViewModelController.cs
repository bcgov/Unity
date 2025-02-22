﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Unity.Flex.Worksheets;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex.Web.Views.Shared.Components.Worksheets
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("Flex/Widgets/Worksheet")]
    public class WorksheetWidgetViewModelController(IWorksheetAppService worksheetAppService) : AbpController
    {
        [HttpGet]
        [Route("Refresh")]
        public async Task<IActionResult> Worksheet([FromQuery] Guid worksheetId)
        {
            // Check if the model state is valid
            if (!ModelState.IsValid)
            {       
                Logger.LogWarning("Invalid model state for WorksheetWidgetViewModelController:Refresh");
                return ViewComponent(typeof(WorksheetWidget));
            }

            // If the model state is valid, render the view component
            var worksheet = await worksheetAppService.GetAsync(worksheetId);
            return ViewComponent(typeof(WorksheetWidget), new { worksheetDto = worksheet });
        }
    }
}

