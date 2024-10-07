using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Payments.Web.Views.Shared.Components.SupplierInfo
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("Payments/Widget/SupplierInfo")]
    public class SupplierInfoController: AbpController
	{
        protected ILogger logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);

        [HttpGet]
        [Route("Refresh")]
        public IActionResult SupplierInfo(Guid applicantId)
        {
            // Check if the model state is valid
            if (!ModelState.IsValid)
            {       
                logger.LogWarning("Invalid model state for SupplierInfoController");
                return ViewComponent("SupplierInfo");
            }

            // If the model state is valid, render the view component
            return ViewComponent("SupplierInfo", new { applicantId });
        }
    }
}

