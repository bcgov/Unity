using AutoMapper;
using Unity.Payments.BatchPaymentRequests;

namespace Unity.Payments;

public class PaymentsApplicationAutoMapperProfile : Profile
{
    public PaymentsApplicationAutoMapperProfile()
    {
        CreateMap<BatchPaymentRequest, BatchPaymentRequestDto>();
        CreateMap<PaymentRequest, PaymentRequestDto>();
        CreateMap<ExpenseApproval, ExpenseApprovalDto>();
    }
}
