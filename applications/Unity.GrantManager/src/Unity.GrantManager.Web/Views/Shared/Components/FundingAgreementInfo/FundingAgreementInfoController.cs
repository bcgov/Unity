using System;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Unity.GrantManager.Web.Views.Shared.Components.FundingAgreementInfo
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("GrantApplications/Widget/FundingAgreementInfo")]
    public class FundingAgreementInfoController : AbpController
	{
        protected ILogger logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);

        [HttpGet]
        [Route("Refresh")]
        public IActionResult FundingAgreementInfo(Guid applicationId, Guid applicationFormVersionId)
        {
            if (!ModelState.IsValid)
            {
                logger.LogWarning("Invalid model state for FundingAgreementInfoController: Refresh");
                return ViewComponent("FundingAgreementInfo");
            }
            return ViewComponent("FundingAgreementInfo", new { applicationId, applicationFormVersionId });
        }
    }
}

