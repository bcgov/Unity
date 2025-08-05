using AutoMapper;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GlobalTag;
using Unity.Payments.Domain.PaymentConfigurations;
using Unity.Payments.Domain.PaymentRequests;
using Unity.Payments.Domain.PaymentTags;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.PaymentConfigurations;
using Unity.Payments.PaymentRequests;
using Unity.Payments.PaymentTags;
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
            .ForMember(dest => dest.CreatorUser, opt => opt.Ignore())
            .ForMember(dest => dest.PaymentTags, opt => opt.MapFrom(src => src.PaymentTags));

        CreateMap<PaymentRequest, PaymentDetailsDto>()
            .ForMember(dest => dest.Site, opt => opt.MapFrom(src => src.Site));
        CreateMap<ExpenseApproval, ExpenseApprovalDto>()
            .ForMember(x => x.DecisionUser, map => map.Ignore());
        CreateMap<Site, SiteDto>()
            .ForMember(dest => dest.PaymentGroup, opt => opt.MapFrom(s => s.PaymentGroup.ToString()));
        CreateMap<Supplier, SupplierDto>();
        CreateMap<PaymentConfiguration, PaymentConfigurationDto>();
        CreateMap<IUserData, PaymentUserDto>();
        CreateMap<Tag, GlobalTagDto>();
        CreateMap<PaymentTag, PaymentTagDto>();

        CreateMap<TagSummaryCount, TagSummaryCountDto>();
    }
}
