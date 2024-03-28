using AutoMapper;
using Unity.Payments.Settings;
using Unity.Payments.Web.Views.Shared.Components.PaymentsSettingsGroup;

namespace Unity.Payments.Web;

public class PaymentsWebAutoMapperProfile : Profile
{
    public PaymentsWebAutoMapperProfile()
    {
        CreateMap<PaymentsSettingsDto, UpdatePaymentsSettingsViewModel>();
    }
}
