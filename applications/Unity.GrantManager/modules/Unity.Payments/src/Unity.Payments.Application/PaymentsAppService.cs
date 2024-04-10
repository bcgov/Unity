using Unity.Payments.Localization;
using Volo.Abp.Application.Services;

namespace Unity.Payments;

public abstract class PaymentsAppService : ApplicationService
{
    protected PaymentsAppService()
    {
        LocalizationResource = typeof(PaymentsResource);
        ObjectMapperContext = typeof(PaymentsApplicationModule);
    }
}
