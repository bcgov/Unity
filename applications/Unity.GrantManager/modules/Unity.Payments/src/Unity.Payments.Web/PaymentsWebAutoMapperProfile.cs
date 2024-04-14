using AutoMapper;
using Unity.Payments.PaymentConfigurations;

namespace Unity.Payments.Web;

public class PaymentsWebAutoMapperProfile : Profile
{
    public PaymentsWebAutoMapperProfile()
    {
        CreateMap<PaymentConfiguration, PaymentConfigurationDto>();
    }
}
