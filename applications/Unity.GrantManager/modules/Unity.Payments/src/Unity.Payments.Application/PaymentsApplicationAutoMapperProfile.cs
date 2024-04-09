using AutoMapper;
using Unity.Payments.BatchPaymentRequests;
using Unity.Payments.Suppliers;
using Unity.Payments.SupplierInfo;

namespace Unity.Payments;

public class PaymentsApplicationAutoMapperProfile : Profile
{
    public PaymentsApplicationAutoMapperProfile()
    {
        CreateMap<BatchPaymentRequest, BatchPaymentRequestDto>();
        CreateMap<PaymentRequest, PaymentRequestDto>();
        CreateMap<ExpenseApproval, ExpenseApprovalDto>();
        CreateMap<Site, SiteDto>()
            .ForMember(dest => dest.PayGroup, opt => opt.MapFrom(s => s.PaymentMethod.ToString()));
    }
}
