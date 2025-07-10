using AutoMapper;
using Unity.Payments.Domain.PaymentRequests;
using Unity.Payments.Domain.PaymentConfigurations;
using Unity.Payments.Domain.Suppliers;
using Volo.Abp.Users;
using Unity.Payments.Domain.PaymentThresholds;

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
        CreateMap<AccountCoding, AccountCodingDto>();
        CreateMap<AccountCodingDto, CreateUpdateAccountCodingDto>();
        CreateMap<CreateUpdateAccountCodingDto, AccountCoding>()
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeleterId, opt => opt.Ignore())
            .ForMember(dest => dest.DeletionTime, opt => opt.Ignore())
            .ForMember(dest => dest.LastModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.LastModifierId, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.CreatorId, opt => opt.Ignore())
            .ForMember(dest => dest.ExtraProperties, opt => opt.Ignore())
            .ForMember(dest => dest.ConcurrencyStamp, opt => opt.Ignore())
            .ForMember(dest => dest.Id, opt => opt.Ignore());        
        CreateMap<PaymentThresholdDto, UpdatePaymentThresholdDto>()
         .ForMember(dest => dest.UserName, opt => opt.Ignore());
        CreateMap<PaymentThreshold, PaymentThresholdDto>()
                 .ForMember(dest => dest.UserName, opt => opt.Ignore());
        CreateMap<UpdatePaymentThresholdDto, PaymentThreshold>()
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
            .ForMember(dest => dest.IsDeleted, opt => opt.Ignore())
            .ForMember(dest => dest.DeleterId, opt => opt.Ignore())
            .ForMember(dest => dest.DeletionTime, opt => opt.Ignore())
            .ForMember(dest => dest.LastModificationTime, opt => opt.Ignore())
            .ForMember(dest => dest.LastModifierId, opt => opt.Ignore())
            .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
            .ForMember(dest => dest.CreatorId, opt => opt.Ignore())
            .ForMember(dest => dest.ExtraProperties, opt => opt.Ignore())
            .ForMember(dest => dest.ConcurrencyStamp, opt => opt.Ignore())
            .ForMember(dest => dest.Id, opt => opt.Ignore());
        CreateMap<IUserData, PaymentUserDto>();
        CreateMap<Tag, GlobalTagDto>();
        CreateMap<PaymentTag, PaymentTagDto>();

        CreateMap<TagSummaryCount, TagSummaryCountDto>();
    }
}
