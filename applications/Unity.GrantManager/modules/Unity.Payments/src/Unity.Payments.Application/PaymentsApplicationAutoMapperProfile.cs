using AutoMapper;
using Unity.Payments.BatchPaymentRequests;
using Unity.Payments.Domain.BatchPaymentRequests;
using Unity.Payments.Domain.PaymentConfigurations;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.PaymentConfigurations;
using Unity.Payments.Suppliers;

namespace Unity.Payments;

public class PaymentsApplicationAutoMapperProfile : Profile
{
    public PaymentsApplicationAutoMapperProfile()
    {
        CreateMap<BatchPaymentRequest, BatchPaymentRequestDto>();
        CreateMap<PaymentRequest, PaymentRequestDto>();
        CreateMap<ExpenseApproval, ExpenseApprovalDto>();
        CreateMap<Site, SiteDto>()
            .ForMember(dest => dest.PaymentGroup, opt => opt.MapFrom(s => s.PaymentGroup.ToString()));
        CreateMap<Supplier, SupplierDto>();
        CreateMap<PaymentConfiguration, PaymentConfigurationDto>();
    }
}
