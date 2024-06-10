using Microsoft.AspNetCore.Mvc;
using System;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Payments.Web.Views.Shared.Components.PaymentInfo
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("Payemnts/Widget/PaymentInfo")]
    public class PaymentInfoController : AbpController
    {
        [HttpGet]
        [Route("Refresh")]
        public IActionResult PaymentInfo(Guid applicationId)
        {
            return ViewComponent("PaymentInfo", new { applicationId });
        }
    }
}
