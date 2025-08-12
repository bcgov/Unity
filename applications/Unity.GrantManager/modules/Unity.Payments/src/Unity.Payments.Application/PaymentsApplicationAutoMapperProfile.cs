using AutoMapper;
using Unity.GrantManager.Applications;
using Unity.GrantManager.GlobalTag;
using Unity.GrantManager.Payments;
using Unity.Payments.Domain.AccountCodings;
using Unity.Payments.Domain.PaymentConfigurations;
using Unity.Payments.Domain.PaymentRequests;
using Unity.Payments.Domain.PaymentTags;
using Unity.Payments.Domain.PaymentThresholds;
using Unity.Payments.Domain.Suppliers;
using Unity.Payments.PaymentConfigurations;
using Unity.Payments.PaymentRequests;
using Unity.Payments.PaymentTags;
using Unity.Payments.PaymentThresholds;
using Unity.Payments.Suppliers;
using Volo.Abp.Users;


namespace Unity.Payments;

public class PaymentsApplicationAutoMapperProfile : Profile
{
    public PaymentsApplicationAutoMapperProfile()
    {
        CreateMap<PaymentRequest, PaymentRequestDto>()
            .ForMember(dest => dest.ErrorSummary, opt => opt.Ignore())
            .ForMember(dest => dest.AccountCoding, opt => opt.MapFrom(src => src.AccountCoding))
            .ForMember(dest => dest.AccountCodingDisplay, opt => opt.Ignore())
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

        CreateMap<CreateUpdateAccountCodingDto, AccountCoding>()
           .ForMember(dest => dest.TenantId, opt => opt.Ignore())
           .ForMember(dest => dest.LastModificationTime, opt => opt.Ignore())
           .ForMember(dest => dest.LastModifierId, opt => opt.Ignore())
           .ForMember(dest => dest.CreationTime, opt => opt.Ignore())
           .ForMember(dest => dest.CreatorId, opt => opt.Ignore())
           .ForMember(dest => dest.ExtraProperties, opt => opt.Ignore())
           .ForMember(dest => dest.ConcurrencyStamp, opt => opt.Ignore())
           .ForMember(dest => dest.Id, opt => opt.Ignore());
        CreateMap<PaymentConfiguration, PaymentConfigurationDto>();
        CreateMap<AccountCoding, AccountCodingDto>();
        CreateMap<AccountCodingDto, CreateUpdateAccountCodingDto>();
        CreateMap<CreateUpdateAccountCodingDto, AccountCoding>()
            .ForMember(dest => dest.TenantId, opt => opt.Ignore())
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
