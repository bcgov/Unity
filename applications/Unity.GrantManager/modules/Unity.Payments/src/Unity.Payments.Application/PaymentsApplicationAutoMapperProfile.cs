using AutoMapper;
using Unity.Payments.PaymentRequests;
using Unity.Payments.Domain.PaymentRequests;
using Unity.Payments.Domain.PaymentConfigurations;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.PaymentConfigurations;
using Unity.Payments.Suppliers;

namespace Unity.Payments;

public class PaymentsApplicationAutoMapperProfile : Profile
{
    public PaymentsApplicationAutoMapperProfile()
    {
        CreateMap<PaymentRequest, PaymentRequestDto>();
        CreateMap<PaymentRequest, PaymentDetailsDto>();
        CreateMap<ExpenseApproval, ExpenseApprovalDto>();
        CreateMap<Site, SiteDto>()
            .ForMember(dest => dest.PaymentGroup, opt => opt.MapFrom(s => s.PaymentGroup.ToString()));
        CreateMap<Supplier, SupplierDto>();
        CreateMap<PaymentConfiguration, PaymentConfigurationDto>();
    }
}
