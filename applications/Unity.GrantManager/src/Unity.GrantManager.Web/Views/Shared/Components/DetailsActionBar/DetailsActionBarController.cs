using Microsoft.AspNetCore.Mvc;
using System;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.GrantManager.Web.Views.Shared.Components.DetailsActionBar;


[ApiExplorerSettings(IgnoreApi = true)]
[Route("GrantApplications/Widgets/DetailsActionBar")]
public class DetailsActionBarController : AbpController
{
    [HttpGet]
    [Route("Refresh")]
    public IActionResult Refresh(Guid applicationId)
    {
        return ViewComponent(typeof(DetailsActionBar), new { applicationId });
    }
}

