using AutoMapper;
using Unity.Payments.PaymentSettings;

namespace Unity.Payments.Web;

public class PaymentsWebAutoMapperProfile : Profile
{
    public PaymentsWebAutoMapperProfile()
    {
        CreateMap<PaymentSetting, PaymentSettingsDto>();
    }
}
