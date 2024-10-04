using Unity.Payments.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Payments.Controllers;

public abstract class PaymentsController : AbpControllerBase
{
    protected PaymentsController()
    {
        LocalizationResource = typeof(PaymentsResource);
    }
}
