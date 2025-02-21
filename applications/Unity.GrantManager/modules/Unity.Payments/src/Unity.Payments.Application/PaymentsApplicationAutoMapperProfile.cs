using AutoMapper;
using Unity.Payments.PaymentRequests;
using Unity.Payments.Domain.PaymentRequests;
using Unity.Payments.Domain.PaymentConfigurations;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.PaymentConfigurations;
using Unity.Payments.Suppliers;
using Volo.Abp.Users;

namespace Unity.Payments;

public class PaymentsApplicationAutoMapperProfile : Profile
{
    public PaymentsApplicationAutoMapperProfile()
    {
        CreateMap<PaymentRequest, PaymentRequestDto>()
            .ForMember(dest => dest.ErrorSummary, options => options.Ignore())
            .ForMember(dest => dest.Site, opt => opt.MapFrom(src => src.Site))
            .ForMember(dest => dest.CreatorUser, opt => opt.Ignore());

        CreateMap<PaymentRequest, PaymentDetailsDto>()
            .ForMember(dest => dest.Site, opt => opt.MapFrom(src => src.Site));
        CreateMap<ExpenseApproval, ExpenseApprovalDto>()
            .ForMember(x => x.DecisionUser, map => map.Ignore());
        CreateMap<Site, SiteDto>()
            .ForMember(dest => dest.PaymentGroup, opt => opt.MapFrom(s => s.PaymentGroup.ToString()));
        CreateMap<Supplier, SupplierDto>();
        CreateMap<PaymentConfiguration, PaymentConfigurationDto>();
        CreateMap<IUserData, PaymentUserDto>();
    }
}
