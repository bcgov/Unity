using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Payments.Web.Views.Shared.Components.PaymentInfo
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("Payemnts/Widget/PaymentInfo")]
    public class PaymentInfoController : AbpController
    {
        protected ILogger logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);

        [HttpGet]
        [Route("Refresh")]
        public IActionResult PaymentInfo(Guid applicationId)
        {
            // Check if the model state is valid
            if (!ModelState.IsValid)
            {       
                logger.LogWarning("Invalid model state for PaymentInfoController:Refresh");
                return ViewComponent("PaymentInfo");
            }

            // If the model state is valid, render the view component
            return ViewComponent("PaymentInfo", new { applicationId });
        }
    }
}
