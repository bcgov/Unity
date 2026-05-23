using Riok.Mapperly.Abstractions;
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
using Volo.Abp.Mapperly;
using Volo.Abp.Users;

namespace Unity.Payments;

[Mapper(AllowNullPropertyAssignment = true)]
public partial class PaymentRequestToPaymentRequestDtoMapper : MapperBase<PaymentRequest, PaymentRequestDto>
{
    [MapperIgnoreTarget(nameof(PaymentRequestDto.ErrorSummary))]
    [MapperIgnoreTarget(nameof(PaymentRequestDto.AccountCodingDisplay))]
    [MapperIgnoreTarget(nameof(PaymentRequestDto.CreatorUser))]
    public override partial PaymentRequestDto Map(PaymentRequest source);

    [MapperIgnoreTarget(nameof(PaymentRequestDto.ErrorSummary))]
    [MapperIgnoreTarget(nameof(PaymentRequestDto.AccountCodingDisplay))]
    [MapperIgnoreTarget(nameof(PaymentRequestDto.CreatorUser))]
    public override partial void Map(PaymentRequest source, PaymentRequestDto destination);

    [MapperIgnoreTarget(nameof(ExpenseApprovalDto.DecisionUser))]
    private partial ExpenseApprovalDto MapExpenseApproval(ExpenseApproval source);
}

[Mapper]
public partial class PaymentRequestToPaymentDetailsDtoMapper : MapperBase<PaymentRequest, PaymentDetailsDto>
{
    public override partial PaymentDetailsDto Map(PaymentRequest source);
    public override partial void Map(PaymentRequest source, PaymentDetailsDto destination);

    [MapperIgnoreTarget(nameof(ExpenseApprovalDto.DecisionUser))]
    private partial ExpenseApprovalDto MapExpenseApproval(ExpenseApproval source);
}

[Mapper]
public partial class ExpenseApprovalToExpenseApprovalDtoMapper : MapperBase<ExpenseApproval, ExpenseApprovalDto>
{
    [MapperIgnoreTarget(nameof(ExpenseApprovalDto.DecisionUser))]
    public override partial ExpenseApprovalDto Map(ExpenseApproval source);

    [MapperIgnoreTarget(nameof(ExpenseApprovalDto.DecisionUser))]
    public override partial void Map(ExpenseApproval source, ExpenseApprovalDto destination);
}

[Mapper]
public partial class SiteToSiteDtoMapper : MapperBase<Site, SiteDto>
{
    public override partial SiteDto Map(Site source);
    public override partial void Map(Site source, SiteDto destination);
}

[Mapper]
public partial class SupplierToSupplierDtoMapper : MapperBase<Supplier, SupplierDto>
{
    public override partial SupplierDto Map(Supplier source);
    public override partial void Map(Supplier source, SupplierDto destination);
}

public class CreateUpdateAccountCodingDtoToAccountCodingMapper : MapperBase<CreateUpdateAccountCodingDto, AccountCoding>
{
    public override AccountCoding Map(CreateUpdateAccountCodingDto source)
    {
        return AccountCoding.Create(
            source.MinistryClient!,
            source.Responsibility!,
            source.ServiceLine!,
            source.Stob!,
            source.ProjectNumber!,
            source.Description);
    }

    public override void Map(CreateUpdateAccountCodingDto source, AccountCoding destination)
    {
        destination.Update(
            source.MinistryClient!,
            source.Responsibility!,
            source.ServiceLine!,
            source.Stob!,
            source.ProjectNumber!,
            source.Description);
    }
}

[Mapper]
public partial class PaymentConfigurationToPaymentConfigurationDtoMapper : MapperBase<PaymentConfiguration, PaymentConfigurationDto>
{
    public override partial PaymentConfigurationDto Map(PaymentConfiguration source);
    public override partial void Map(PaymentConfiguration source, PaymentConfigurationDto destination);
}

[Mapper]
public partial class AccountCodingToAccountCodingDtoMapper : MapperBase<AccountCoding, AccountCodingDto>
{
    public override partial AccountCodingDto Map(AccountCoding source);
    public override partial void Map(AccountCoding source, AccountCodingDto destination);
}

[Mapper]
public partial class AccountCodingDtoToCreateUpdateAccountCodingDtoMapper : MapperBase<AccountCodingDto, CreateUpdateAccountCodingDto>
{
    public override partial CreateUpdateAccountCodingDto Map(AccountCodingDto source);
    public override partial void Map(AccountCodingDto source, CreateUpdateAccountCodingDto destination);
}

[Mapper]
public partial class PaymentThresholdDtoToUpdatePaymentThresholdDtoMapper : MapperBase<PaymentThresholdDto, UpdatePaymentThresholdDto>
{
    [MapperIgnoreTarget(nameof(UpdatePaymentThresholdDto.UserName))]
    public override partial UpdatePaymentThresholdDto Map(PaymentThresholdDto source);

    [MapperIgnoreTarget(nameof(UpdatePaymentThresholdDto.UserName))]
    public override partial void Map(PaymentThresholdDto source, UpdatePaymentThresholdDto destination);
}

[Mapper]
public partial class PaymentThresholdToPaymentThresholdDtoMapper : MapperBase<PaymentThreshold, PaymentThresholdDto>
{
    [MapperIgnoreTarget(nameof(PaymentThresholdDto.UserName))]
    public override partial PaymentThresholdDto Map(PaymentThreshold source);

    [MapperIgnoreTarget(nameof(PaymentThresholdDto.UserName))]
    public override partial void Map(PaymentThreshold source, PaymentThresholdDto destination);
}

[Mapper]
public partial class UpdatePaymentThresholdDtoToPaymentThresholdMapper : MapperBase<UpdatePaymentThresholdDto, PaymentThreshold>
{
    [MapperIgnoreTarget(nameof(PaymentThreshold.TenantId))]
    [MapperIgnoreTarget(nameof(PaymentThreshold.IsDeleted))]
    [MapperIgnoreTarget(nameof(PaymentThreshold.DeleterId))]
    [MapperIgnoreTarget(nameof(PaymentThreshold.DeletionTime))]
    [MapperIgnoreTarget(nameof(PaymentThreshold.LastModificationTime))]
    [MapperIgnoreTarget(nameof(PaymentThreshold.LastModifierId))]
    [MapperIgnoreTarget(nameof(PaymentThreshold.CreationTime))]
    [MapperIgnoreTarget(nameof(PaymentThreshold.CreatorId))]
    [MapperIgnoreTarget(nameof(PaymentThreshold.ConcurrencyStamp))]
    public override partial PaymentThreshold Map(UpdatePaymentThresholdDto source);

    [MapperIgnoreTarget(nameof(PaymentThreshold.TenantId))]
    [MapperIgnoreTarget(nameof(PaymentThreshold.IsDeleted))]
    [MapperIgnoreTarget(nameof(PaymentThreshold.DeleterId))]
    [MapperIgnoreTarget(nameof(PaymentThreshold.DeletionTime))]
    [MapperIgnoreTarget(nameof(PaymentThreshold.LastModificationTime))]
    [MapperIgnoreTarget(nameof(PaymentThreshold.LastModifierId))]
    [MapperIgnoreTarget(nameof(PaymentThreshold.CreationTime))]
    [MapperIgnoreTarget(nameof(PaymentThreshold.CreatorId))]
    [MapperIgnoreTarget(nameof(PaymentThreshold.ConcurrencyStamp))]
    public override partial void Map(UpdatePaymentThresholdDto source, PaymentThreshold destination);
}

[Mapper]
public partial class UserDataToPaymentUserDtoMapper : MapperBase<IUserData, PaymentUserDto>
{
    public override partial PaymentUserDto Map(IUserData source);
    public override partial void Map(IUserData source, PaymentUserDto destination);
}

[Mapper]
public partial class TagToGlobalTagDtoMapper : MapperBase<Tag, GlobalTagDto>
{
    public override partial GlobalTagDto Map(Tag source);
    public override partial void Map(Tag source, GlobalTagDto destination);
}

[Mapper]
public partial class PaymentTagToPaymentTagDtoMapper : MapperBase<PaymentTag, PaymentTagDto>
{
    public override partial PaymentTagDto Map(PaymentTag source);
    public override partial void Map(PaymentTag source, PaymentTagDto destination);
}

[Mapper]
public partial class TagSummaryCountToTagSummaryCountDtoMapper : MapperBase<TagSummaryCount, TagSummaryCountDto>
{
    public override partial TagSummaryCountDto Map(TagSummaryCount source);
    public override partial void Map(TagSummaryCount source, TagSummaryCountDto destination);
}
