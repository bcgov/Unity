using Unity.Payments.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace Unity.Payments;

public abstract class PaymentsController : AbpControllerBase
{
    protected PaymentsController()
    {
        LocalizationResource = typeof(PaymentsResource);
    }
}
