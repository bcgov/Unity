using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;


namespace Unity.GrantManager.Web.Views.Shared.Components.PaymentConfiguration
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Route("GrantApplications/PaymentConfiguration")]
    public class PaymentConfigurationController : AbpController
    {
        protected ILogger logger => LazyServiceProvider.LazyGetService<ILogger>(provider => LoggerFactory?.CreateLogger(GetType().FullName!) ?? NullLogger.Instance);
    }
}
