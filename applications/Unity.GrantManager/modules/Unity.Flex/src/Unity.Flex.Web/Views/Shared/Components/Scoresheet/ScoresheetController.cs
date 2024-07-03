using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Flex.Web.Views.Shared.Components.Scoresheet
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("Flex/Widget/Scoresheet")]
    public class ScoresheetController: AbpController
	{
        [HttpGet]
        [Route("Refresh")]
        public IActionResult Scoresheet()
        {
            return ViewComponent("Scoresheet");
        }
    }
}

