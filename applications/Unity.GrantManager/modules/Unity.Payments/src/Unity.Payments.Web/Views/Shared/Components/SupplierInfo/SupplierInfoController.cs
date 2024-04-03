using System;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Payments.Web.Views.Shared.Components.SupplierInfo
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("Payments/Widget/SupplierInfo")]
    public class ApplicantInfoController: AbpController
	{
        [HttpGet]
        [Route("Refresh")]
        public IActionResult ApplicantInfo(Guid applicationId)
        {
            return ViewComponent("SupplierInfo", new { applicationId });
        }
    }
}

