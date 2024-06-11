using Unity.Payments.Localization;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Localization;

namespace Unity.Payments.Controllers;

public abstract class PaymentsController : AbpControllerBase
{
    protected PaymentsController()
    {
        LocalizationResource = typeof(PaymentsResource);
    }
}
