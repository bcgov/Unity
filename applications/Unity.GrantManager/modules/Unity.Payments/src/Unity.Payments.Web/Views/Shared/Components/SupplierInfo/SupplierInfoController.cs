using System;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Payments.Web.Views.Shared.Components.SupplierInfo
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("Payments/Widget/SupplierInfo")]
    public class SupplierInfoController: AbpController
	{
        [HttpGet]
        [Route("Refresh")]
        public IActionResult SupplierInfo(Guid applicantId)
        {
            return ViewComponent("SupplierInfo", new { applicantId });
        }
    }
}

