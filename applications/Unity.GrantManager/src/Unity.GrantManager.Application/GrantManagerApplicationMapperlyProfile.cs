using Riok.Mapperly.Abstractions;
using System;
using System.Reflection;
using Unity.GrantManager.ApplicantProfile;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Assessments;
using Unity.GrantManager.Attachments;
using Unity.GrantManager.Comments;
using Unity.GrantManager.Events;
using Unity.GrantManager.Forms;
using Unity.GrantManager.GlobalTag;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Identity;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.Integrations;
using Unity.GrantManager.Locality;
using Unity.GrantManager.Zones;
using Unity.Payments.Domain.AccountCodings;
using Unity.Payments.PaymentRequests;
using Volo.Abp.Mapperly;

namespace Unity.GrantManager;

// ---------------------------------------------------------------------------
// Direct (forward) mappers — equivalent to AutoMapper CreateMap<TSrc, TDst>().
// ---------------------------------------------------------------------------

[Mapper] public partial class CreateUpdateDynamicUrlDtoToDynamicUrlMapper : MapperBase<CreateUpdateDynamicUrlDto, DynamicUrl> { public override partial DynamicUrl Map(CreateUpdateDynamicUrlDto source); public override partial void Map(CreateUpdateDynamicUrlDto source, DynamicUrl destination); }
[Mapper] public partial class CreateUpdateDynamicUrlDtoToDynamicUrlDtoMapper : MapperBase<CreateUpdateDynamicUrlDto, DynamicUrlDto> { public override partial DynamicUrlDto Map(CreateUpdateDynamicUrlDto source); public override partial void Map(CreateUpdateDynamicUrlDto source, DynamicUrlDto destination); }
[Mapper] public partial class DynamicUrlDtoToCreateUpdateDynamicUrlDtoMapper : MapperBase<DynamicUrlDto, CreateUpdateDynamicUrlDto> { public override partial CreateUpdateDynamicUrlDto Map(DynamicUrlDto source); public override partial void Map(DynamicUrlDto source, CreateUpdateDynamicUrlDto destination); }
[Mapper] public partial class DynamicUrlToDynamicUrlDtoMapper : MapperBase<DynamicUrl, DynamicUrlDto> { public override partial DynamicUrlDto Map(DynamicUrl source); public override partial void Map(DynamicUrl source, DynamicUrlDto destination); }
[Mapper] public partial class DynamicUrlToCreateUpdateDynamicUrlDtoMapper : MapperBase<DynamicUrl, CreateUpdateDynamicUrlDto> { public override partial CreateUpdateDynamicUrlDto Map(DynamicUrl source); public override partial void Map(DynamicUrl source, CreateUpdateDynamicUrlDto destination); }

[Mapper] public partial class ApplicationToGrantApplicationDtoMapper : MapperBase<Application, GrantApplicationDto> { public override partial GrantApplicationDto Map(Application source); public override partial void Map(Application source, GrantApplicationDto destination); }
[Mapper] public partial class ApplicationAssignmentToGrantApplicationAssigneeDtoMapper : MapperBase<ApplicationAssignment, GrantApplicationAssigneeDto> { public override partial GrantApplicationAssigneeDto Map(ApplicationAssignment source); public override partial void Map(ApplicationAssignment source, GrantApplicationAssigneeDto destination); }

[Mapper]
public partial class PersonToGrantApplicationAssigneeDtoMapper : MapperBase<Person, GrantApplicationAssigneeDto>
{
    [MapProperty(nameof(Person.Id), nameof(GrantApplicationAssigneeDto.AssigneeId))]
    [MapperIgnoreTarget(nameof(GrantApplicationAssigneeDto.ApplicationId))]
    [MapperIgnoreTarget(nameof(GrantApplicationAssigneeDto.Duty))]
    public override partial GrantApplicationAssigneeDto Map(Person source);

    [MapProperty(nameof(Person.Id), nameof(GrantApplicationAssigneeDto.AssigneeId))]
    [MapperIgnoreTarget(nameof(GrantApplicationAssigneeDto.ApplicationId))]
    [MapperIgnoreTarget(nameof(GrantApplicationAssigneeDto.Duty))]
    public override partial void Map(Person source, GrantApplicationAssigneeDto destination);
}

[Mapper] public partial class ApplicationStatusToApplicationStatusDtoMapper : MapperBase<ApplicationStatus, ApplicationStatusDto> { public override partial ApplicationStatusDto Map(ApplicationStatus source); public override partial void Map(ApplicationStatus source, ApplicationStatusDto destination); }

[Mapper]
public partial class AssessmentCommentToCommentDtoMapper : MapperBase<AssessmentComment, CommentDto>
{
    [MapProperty(nameof(AssessmentComment.AssessmentId), nameof(CommentDto.OwnerId))]
    public override partial CommentDto Map(AssessmentComment source);

    [MapProperty(nameof(AssessmentComment.AssessmentId), nameof(CommentDto.OwnerId))]
    public override partial void Map(AssessmentComment source, CommentDto destination);
}

[Mapper]
public partial class ApplicationCommentToCommentDtoMapper : MapperBase<ApplicationComment, CommentDto>
{
    [MapProperty(nameof(ApplicationComment.ApplicationId), nameof(CommentDto.OwnerId))]
    public override partial CommentDto Map(ApplicationComment source);

    [MapProperty(nameof(ApplicationComment.ApplicationId), nameof(CommentDto.OwnerId))]
    public override partial void Map(ApplicationComment source, CommentDto destination);
}

[Mapper]
public partial class ApplicantCommentToCommentDtoMapper : MapperBase<ApplicantComment, CommentDto>
{
    [MapProperty(nameof(ApplicantComment.ApplicantId), nameof(CommentDto.OwnerId))]
    public override partial CommentDto Map(ApplicantComment source);

    [MapProperty(nameof(ApplicantComment.ApplicantId), nameof(CommentDto.OwnerId))]
    public override partial void Map(ApplicantComment source, CommentDto destination);
}

public class CommentBaseToCommentDtoMapper : MapperBase<CommentBase, CommentDto>
{
    public override CommentDto Map(CommentBase source) => source switch
    {
        ApplicationComment a => new ApplicationCommentToCommentDtoMapper().Map(a),
        AssessmentComment a => new AssessmentCommentToCommentDtoMapper().Map(a),
        ApplicantComment a => new ApplicantCommentToCommentDtoMapper().Map(a),
        _ => throw new System.NotSupportedException($"Unsupported comment type: {source.GetType().FullName}")
    };

    public override void Map(CommentBase source, CommentDto destination)
    {
        switch (source)
        {
            case ApplicationComment a: new ApplicationCommentToCommentDtoMapper().Map(a, destination); break;
            case AssessmentComment a: new AssessmentCommentToCommentDtoMapper().Map(a, destination); break;
            case ApplicantComment a: new ApplicantCommentToCommentDtoMapper().Map(a, destination); break;
            default: throw new System.NotSupportedException($"Unsupported comment type: {source.GetType().FullName}");
        }
    }
}

[Mapper]
public partial class CommentListItemToCommentDtoMapper : MapperBase<CommentListItem, CommentDto>
{
    [MapProperty(nameof(CommentListItem.CommenterBadge), nameof(CommentDto.Badge))]
    [MapProperty(nameof(CommentListItem.CommenterDisplayName), nameof(CommentDto.Commenter))]
    public override partial CommentDto Map(CommentListItem source);

    [MapProperty(nameof(CommentListItem.CommenterBadge), nameof(CommentDto.Badge))]
    [MapProperty(nameof(CommentListItem.CommenterDisplayName), nameof(CommentDto.Commenter))]
    public override partial void Map(CommentListItem source, CommentDto destination);
}

[Mapper]
public partial class AssessmentToAssessmentDtoMapper : MapperBase<Assessment, AssessmentDto>
{
    [MapProperty(nameof(Assessment.CreationTime), nameof(AssessmentDto.StartDate))]
    public override partial AssessmentDto Map(Assessment source);

    [MapProperty(nameof(Assessment.CreationTime), nameof(AssessmentDto.StartDate))]
    public override partial void Map(Assessment source, AssessmentDto destination);
}

[Mapper]
public partial class AssessmentWithAssessorQueryResultItemToAssessmentListItemDtoMapper : MapperBase<AssessmentWithAssessorQueryResultItem, AssessmentListItemDto>
{
    [MapperIgnoreTarget(nameof(AssessmentListItemDto.SubTotal))]
    public override partial AssessmentListItemDto Map(AssessmentWithAssessorQueryResultItem source);

    [MapperIgnoreTarget(nameof(AssessmentListItemDto.SubTotal))]
    public override partial void Map(AssessmentWithAssessorQueryResultItem source, AssessmentListItemDto destination);
}

[Mapper]
public partial class ApplicationChefsFileAttachmentToDtoMapper : MapperBase<ApplicationChefsFileAttachment, ApplicationChefsFileAttachmentDto>
{
    [MapProperty(nameof(ApplicationChefsFileAttachment.CreationTime), nameof(ApplicationChefsFileAttachmentDto.CreatedTime))]
    [MapProperty(nameof(ApplicationChefsFileAttachment.LastModificationTime), nameof(ApplicationChefsFileAttachmentDto.UpdatedTime))]
    public override partial ApplicationChefsFileAttachmentDto Map(ApplicationChefsFileAttachment source);

    [MapProperty(nameof(ApplicationChefsFileAttachment.CreationTime), nameof(ApplicationChefsFileAttachmentDto.CreatedTime))]
    [MapProperty(nameof(ApplicationChefsFileAttachment.LastModificationTime), nameof(ApplicationChefsFileAttachmentDto.UpdatedTime))]
    public override partial void Map(ApplicationChefsFileAttachment source, ApplicationChefsFileAttachmentDto destination);
}

[Mapper] public partial class ApplicationAttachmentToDtoMapper : MapperBase<ApplicationAttachment, ApplicationAttachmentDto> { public override partial ApplicationAttachmentDto Map(ApplicationAttachment source); public override partial void Map(ApplicationAttachment source, ApplicationAttachmentDto destination); }
[Mapper] public partial class IntakeToIntakeDtoMapper : MapperBase<Intakes.Intake, IntakeDto> { public override partial IntakeDto Map(Intakes.Intake source); public override partial void Map(Intakes.Intake source, IntakeDto destination); }
[Mapper] public partial class ApplicationFormToDtoMapper : MapperBase<ApplicationForm, ApplicationFormDto> { public override partial ApplicationFormDto Map(ApplicationForm source); public override partial void Map(ApplicationForm source, ApplicationFormDto destination); }
[Mapper] public partial class ApplicationFormDtoToEntityMapper : MapperBase<ApplicationFormDto, ApplicationForm> { public override partial ApplicationForm Map(ApplicationFormDto source); public override partial void Map(ApplicationFormDto source, ApplicationForm destination); }
[Mapper] public partial class ApplicationFormVersionToDtoMapper : MapperBase<ApplicationFormVersion, ApplicationFormVersionDto> { public override partial ApplicationFormVersionDto Map(ApplicationFormVersion source); public override partial void Map(ApplicationFormVersion source, ApplicationFormVersionDto destination); }
[Mapper] public partial class ApplicationFormVersionDtoToEntityMapper : MapperBase<ApplicationFormVersionDto, ApplicationFormVersion> { public override partial ApplicationFormVersion Map(ApplicationFormVersionDto source); public override partial void Map(ApplicationFormVersionDto source, ApplicationFormVersion destination); }
[Mapper] public partial class CreateUpdateApplicationFormVersionDtoToEntityMapper : MapperBase<CreateUpdateApplicationFormVersionDto, ApplicationFormVersion> { public override partial ApplicationFormVersion Map(CreateUpdateApplicationFormVersionDto source); public override partial void Map(CreateUpdateApplicationFormVersionDto source, ApplicationFormVersion destination); }
[Mapper] public partial class CreateUpdateIntakeDtoToEntityMapper : MapperBase<CreateUpdateIntakeDto, Intakes.Intake> { public override partial Intakes.Intake Map(CreateUpdateIntakeDto source); public override partial void Map(CreateUpdateIntakeDto source, Intakes.Intake destination); }
[Mapper] public partial class CreateUpdateApplicationFormDtoToEntityMapper : MapperBase<CreateUpdateApplicationFormDto, ApplicationForm> { public override partial ApplicationForm Map(CreateUpdateApplicationFormDto source); public override partial void Map(CreateUpdateApplicationFormDto source, ApplicationForm destination); }
[Mapper] public partial class AssessmentAttachmentToDtoMapper : MapperBase<AssessmentAttachment, AssessmentAttachmentDto> { public override partial AssessmentAttachmentDto Map(AssessmentAttachment source); public override partial void Map(AssessmentAttachment source, AssessmentAttachmentDto destination); }
[Mapper] public partial class ApplicationActionResultItemToDtoMapper : MapperBase<ApplicationActionResultItem, ApplicationActionDto> { public override partial ApplicationActionDto Map(ApplicationActionResultItem source); public override partial void Map(ApplicationActionResultItem source, ApplicationActionDto destination); }
[Mapper] public partial class EventSubscriptionToDtoMapper : MapperBase<EventSubscription, EventSubscriptionDto> { public override partial EventSubscriptionDto Map(EventSubscription source); public override partial void Map(EventSubscription source, EventSubscriptionDto destination); }
[Mapper] public partial class EventSubscriptionDtoToEntityMapper : MapperBase<EventSubscriptionDto, EventSubscription> { public override partial EventSubscription Map(EventSubscriptionDto source); public override partial void Map(EventSubscriptionDto source, EventSubscription destination); }
[Mapper] public partial class SectorToDtoMapper : MapperBase<Sector, SectorDto> { public override partial SectorDto Map(Sector source); public override partial void Map(Sector source, SectorDto destination); }
[Mapper] public partial class SubSectorToDtoMapper : MapperBase<SubSector, SubSectorDto> { public override partial SubSectorDto Map(SubSector source); public override partial void Map(SubSector source, SubSectorDto destination); }
[Mapper] public partial class EconomicRegionToDtoMapper : MapperBase<EconomicRegion, EconomicRegionDto> { public override partial EconomicRegionDto Map(EconomicRegion source); public override partial void Map(EconomicRegion source, EconomicRegionDto destination); }
[Mapper] public partial class ElectoralDistrictToDtoMapper : MapperBase<ElectoralDistrict, ElectoralDistrictDto> { public override partial ElectoralDistrictDto Map(ElectoralDistrict source); public override partial void Map(ElectoralDistrict source, ElectoralDistrictDto destination); }
[Mapper] public partial class CommunityToDtoMapper : MapperBase<Community, CommunityDto> { public override partial CommunityDto Map(Community source); public override partial void Map(Community source, CommunityDto destination); }
[Mapper] public partial class RegionalDistrictToDtoMapper : MapperBase<RegionalDistrict, RegionalDistrictDto> { public override partial RegionalDistrictDto Map(RegionalDistrict source); public override partial void Map(RegionalDistrict source, RegionalDistrictDto destination); }
[Mapper] public partial class ApplicationTagsToDtoMapper : MapperBase<ApplicationTags, ApplicationTagsDto> { public override partial ApplicationTagsDto Map(ApplicationTags source); public override partial void Map(ApplicationTags source, ApplicationTagsDto destination); }
[Mapper] public partial class AIGenerationRequestToDtoMapper : MapperBase<AIGenerationRequest, AIGenerationRequestDto> { public override partial AIGenerationRequestDto Map(AIGenerationRequest source); public override partial void Map(AIGenerationRequest source, AIGenerationRequestDto destination); }
[Mapper] public partial class ApplicantToGrantApplicationApplicantDtoMapper : MapperBase<Applicant, GrantApplicationApplicantDto> { public override partial GrantApplicationApplicantDto Map(Applicant source); public override partial void Map(Applicant source, GrantApplicationApplicantDto destination); }
[Mapper] public partial class ApplicationContactToDtoMapper : MapperBase<ApplicationContact, ApplicationContactDto> { public override partial ApplicationContactDto Map(ApplicationContact source); public override partial void Map(ApplicationContact source, ApplicationContactDto destination); }
[Mapper] public partial class ApplicationContactDtoToEntityMapper : MapperBase<ApplicationContactDto, ApplicationContact> { public override partial ApplicationContact Map(ApplicationContactDto source); public override partial void Map(ApplicationContactDto source, ApplicationContact destination); }
[Mapper] public partial class ApplicationLinkToDtoMapper : MapperBase<ApplicationLink, ApplicationLinksDto> { public override partial ApplicationLinksDto Map(ApplicationLink source); public override partial void Map(ApplicationLink source, ApplicationLinksDto destination); }
[Mapper] public partial class ApplicationLinksDtoToEntityMapper : MapperBase<ApplicationLinksDto, ApplicationLink> { public override partial ApplicationLink Map(ApplicationLinksDto source); public override partial void Map(ApplicationLinksDto source, ApplicationLink destination); }
[Mapper] public partial class ApplicationToGrantApplicationLiteDtoMapper : MapperBase<Application, GrantApplicationLiteDto> { public override partial GrantApplicationLiteDto Map(Application source); public override partial void Map(Application source, GrantApplicationLiteDto destination); }
[Mapper] public partial class ApplicantAddressToDtoMapper : MapperBase<ApplicantAddress, ApplicantAddressDto> { public override partial ApplicantAddressDto Map(ApplicantAddress source); public override partial void Map(ApplicantAddress source, ApplicantAddressDto destination); }
[Mapper] public partial class AccountCodingToAccountCodingDtoGmMapper : MapperBase<AccountCoding, AccountCodingDto> { public override partial AccountCodingDto Map(AccountCoding source); public override partial void Map(AccountCoding source, AccountCodingDto destination); }
[Mapper] public partial class TagToDtoMapper : MapperBase<Tag, TagDto> { public override partial TagDto Map(Tag source); public override partial void Map(Tag source, TagDto destination); }
[Mapper] public partial class TagSummaryCountToDtoGmMapper : MapperBase<TagSummaryCount, TagSummaryCountDto> { public override partial TagSummaryCountDto Map(TagSummaryCount source); public override partial void Map(TagSummaryCount source, TagSummaryCountDto destination); }
[Mapper] public partial class TagUsageSummaryToDtoMapper : MapperBase<TagUsageSummary, TagUsageSummaryDto> { public override partial TagUsageSummaryDto Map(TagUsageSummary source); public override partial void Map(TagUsageSummary source, TagUsageSummaryDto destination); }

[Mapper] public partial class FundingHistoryToDtoMapper : MapperBase<FundingHistory, FundingHistoryDto> { public override partial FundingHistoryDto Map(FundingHistory source); public override partial void Map(FundingHistory source, FundingHistoryDto destination); }
[Mapper] public partial class CreateUpdateFundingHistoryDtoToEntityMapper : MapperBase<CreateUpdateFundingHistoryDto, FundingHistory> { public override partial FundingHistory Map(CreateUpdateFundingHistoryDto source); public override partial void Map(CreateUpdateFundingHistoryDto source, FundingHistory destination); }
[Mapper] public partial class FundingHistoryDtoToEntityMapper : MapperBase<FundingHistoryDto, FundingHistory> { public override partial FundingHistory Map(FundingHistoryDto source); public override partial void Map(FundingHistoryDto source, FundingHistory destination); }

[Mapper] public partial class IssueTrackingToDtoMapper : MapperBase<IssueTracking, IssueTrackingDto> { public override partial IssueTrackingDto Map(IssueTracking source); public override partial void Map(IssueTracking source, IssueTrackingDto destination); }
[Mapper] public partial class CreateUpdateIssueTrackingDtoToEntityMapper : MapperBase<CreateUpdateIssueTrackingDto, IssueTracking> { public override partial IssueTracking Map(CreateUpdateIssueTrackingDto source); public override partial void Map(CreateUpdateIssueTrackingDto source, IssueTracking destination); }
[Mapper] public partial class IssueTrackingDtoToEntityMapper : MapperBase<IssueTrackingDto, IssueTracking> { public override partial IssueTracking Map(IssueTrackingDto source); public override partial void Map(IssueTrackingDto source, IssueTracking destination); }

[Mapper] public partial class AuditHistoryToDtoMapper : MapperBase<AuditHistory, AuditHistoryDto> { public override partial AuditHistoryDto Map(AuditHistory source); public override partial void Map(AuditHistory source, AuditHistoryDto destination); }
[Mapper] public partial class CreateUpdateAuditHistoryDtoToEntityMapper : MapperBase<CreateUpdateAuditHistoryDto, AuditHistory> { public override partial AuditHistory Map(CreateUpdateAuditHistoryDto source); public override partial void Map(CreateUpdateAuditHistoryDto source, AuditHistory destination); }
[Mapper] public partial class AuditHistoryDtoToEntityMapper : MapperBase<AuditHistoryDto, AuditHistory> { public override partial AuditHistory Map(AuditHistoryDto source); public override partial void Map(AuditHistoryDto source, AuditHistory destination); }

[Mapper] public partial class ApplicationToApplicantInfoDtoMapper : MapperBase<Application, ApplicantInfoDto> { public override partial ApplicantInfoDto Map(Application source); public override partial void Map(Application source, ApplicantInfoDto destination); }
[Mapper] public partial class ApplicationToSigningAuthorityDtoMapper : MapperBase<Application, SigningAuthorityDto> { public override partial SigningAuthorityDto Map(Application source); public override partial void Map(Application source, SigningAuthorityDto destination); }
[Mapper] public partial class ApplicantAgentToContactInfoDtoMapper : MapperBase<ApplicantAgent, ContactInfoDto> { public override partial ContactInfoDto Map(ApplicantAgent source); public override partial void Map(ApplicantAgent source, ContactInfoDto destination); }

[Mapper]
public partial class ApplicantToApplicantSummaryDtoMapper : MapperBase<Applicant, ApplicantSummaryDto>
{
    public override partial ApplicantSummaryDto Map(Applicant source);
    public override partial void Map(Applicant source, ApplicantSummaryDto destination);

    private static bool? MapIndigenousOrgInd(string? value) => GrantManagerMapperlyHelpers.IndigenousOrgIndToBool(value);
}

// ---------------------------------------------------------------------------
// Two-way mappers (CreateMap<...>().ReverseMap()).
// ---------------------------------------------------------------------------

[Mapper] public partial class ZoneGroupDefinitionMapper : TwoWayMapperBase<ZoneGroupDefinition, ZoneGroupDefinitionDto> { public override partial ZoneGroupDefinitionDto Map(ZoneGroupDefinition source); public override partial void Map(ZoneGroupDefinition source, ZoneGroupDefinitionDto destination); public override partial ZoneGroupDefinition ReverseMap(ZoneGroupDefinitionDto source); public override partial void ReverseMap(ZoneGroupDefinitionDto source, ZoneGroupDefinition destination); }
[Mapper] public partial class ZoneTabDefinitionMapper : TwoWayMapperBase<ZoneTabDefinition, ZoneTabDefinitionDto> { public override partial ZoneTabDefinitionDto Map(ZoneTabDefinition source); public override partial void Map(ZoneTabDefinition source, ZoneTabDefinitionDto destination); public override partial ZoneTabDefinition ReverseMap(ZoneTabDefinitionDto source); public override partial void ReverseMap(ZoneTabDefinitionDto source, ZoneTabDefinition destination); }
[Mapper] public partial class ZoneDefinitionMapper : TwoWayMapperBase<ZoneDefinition, ZoneDefinitionDto> { public override partial ZoneDefinitionDto Map(ZoneDefinition source); public override partial void Map(ZoneDefinition source, ZoneDefinitionDto destination); public override partial ZoneDefinition ReverseMap(ZoneDefinitionDto source); public override partial void ReverseMap(ZoneDefinitionDto source, ZoneDefinition destination); }

// ---------------------------------------------------------------------------
// Patch mappers — preserve AutoMapper's IgnoreNullAndDefaultValues semantics:
// only non-null, non-default source members overwrite the destination.
// ---------------------------------------------------------------------------

public class UpdateProjectInfoDtoToApplicationMapper : MapperBase<UpdateProjectInfoDto, Application>
{
    public override Application Map(UpdateProjectInfoDto source)
    {
        var destination = new Application();
        Map(source, destination);
        return destination;
    }

    public override void Map(UpdateProjectInfoDto source, Application destination)
        => GrantManagerMapperlyHelpers.CopyNonDefault(source, destination);
}

public class UpdateApplicantInfoDtoToApplicantMapper : MapperBase<UpdateApplicantInfoDto, Applicant>
{
    public override Applicant Map(UpdateApplicantInfoDto source)
    {
        var destination = new Applicant();
        Map(source, destination);
        return destination;
    }

    public override void Map(UpdateApplicantInfoDto source, Applicant destination)
        => GrantManagerMapperlyHelpers.CopyNonDefault(source, destination);
}

public class UpdateApplicantInfoDtoToApplicationMapper : MapperBase<UpdateApplicantInfoDto, Application>
{
    private static readonly string[] IgnoredMembers = [nameof(Application.ElectoralDistrict)];

    public override Application Map(UpdateApplicantInfoDto source)
    {
        var destination = new Application();
        Map(source, destination);
        return destination;
    }

    public override void Map(UpdateApplicantInfoDto source, Application destination)
        => GrantManagerMapperlyHelpers.CopyNonDefault(source, destination, IgnoredMembers);
}

public class SigningAuthorityDtoToApplicationMapper : MapperBase<SigningAuthorityDto, Application>
{
    public override Application Map(SigningAuthorityDto source)
    {
        var destination = new Application();
        Map(source, destination);
        return destination;
    }

    public override void Map(SigningAuthorityDto source, Application destination)
        => GrantManagerMapperlyHelpers.CopyNonDefault(source, destination);
}

public class UpdateApplicantSummaryDtoToApplicantMapper : MapperBase<UpdateApplicantSummaryDto, Applicant>
{
    private static readonly string[] IgnoredMembers = [nameof(Applicant.RedStop), nameof(Applicant.IndigenousOrgInd)];

    public override Applicant Map(UpdateApplicantSummaryDto source)
    {
        var destination = new Applicant();
        Map(source, destination);
        return destination;
    }

    public override void Map(UpdateApplicantSummaryDto source, Applicant destination)
    {
        GrantManagerMapperlyHelpers.CopyNonDefault(source, destination, IgnoredMembers);

        // IndigenousOrgInd: bool? -> "Yes"/"No"/null. Always apply unless the
        // source value itself is the default (null) — matches AutoMapper.
        if (source.IndigenousOrgInd != null)
        {
            destination.IndigenousOrgInd = GrantManagerMapperlyHelpers.BoolToIndigenousOrgInd(source.IndigenousOrgInd);
        }
    }
}

public class ContactInfoDtoToApplicantAgentMapper : MapperBase<ContactInfoDto, ApplicantAgent>
{
    public override ApplicantAgent Map(ContactInfoDto source)
    {
        var destination = new ApplicantAgent();
        Map(source, destination);
        return destination;
    }

    public override void Map(ContactInfoDto source, ApplicantAgent destination)
        => GrantManagerMapperlyHelpers.CopyNonDefault(source, destination);
}

public class UpdateApplicantAddressDtoToApplicantAddressMapper : MapperBase<UpdateApplicantAddressDto, ApplicantAddress>
{
    private static readonly string[] IgnoredMembers = [nameof(ApplicantAddress.Postal)];

    public override ApplicantAddress Map(UpdateApplicantAddressDto source)
    {
        var destination = new ApplicantAddress();
        Map(source, destination);
        return destination;
    }

    public override void Map(UpdateApplicantAddressDto source, ApplicantAddress destination)
    {
        GrantManagerMapperlyHelpers.CopyNonDefault(source, destination, IgnoredMembers);

        // Map PostalCode -> Postal when the source actually has a value.
        if (!string.IsNullOrEmpty(source.PostalCode))
        {
            destination.Postal = source.PostalCode;
        }
    }
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

internal static class GrantManagerMapperlyHelpers
{
    public static bool? IndigenousOrgIndToBool(string? indigenousOrgInd) => indigenousOrgInd switch
    {
        "Yes" => true,
        "No" => false,
        _ => null,
    };

    public static string? BoolToIndigenousOrgInd(bool? indigenousOrgInd) => indigenousOrgInd switch
    {
        true => "Yes",
        false => "No",
        _ => null,
    };

    /// <summary>
    /// Copies all readable members of <paramref name="source"/> whose value is not null
    /// and not the default for their type onto matching writable members of
    /// <paramref name="destination"/>. Mirrors the behavior of AutoMapper's
    /// IgnoreNullAndDefaultValues extension that this codebase relied on.
    /// </summary>
    public static void CopyNonDefault<TSource, TDestination>(
        TSource source,
        TDestination destination,
        params string[] ignoredMembers)
        where TSource : notnull
        where TDestination : notnull
    {
        var sourceProps = typeof(TSource).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var destType = typeof(TDestination);

        foreach (var srcProp in sourceProps)
        {
            if (!srcProp.CanRead)
            {
                continue;
            }

            if (Array.IndexOf(ignoredMembers, srcProp.Name) >= 0)
            {
                continue;
            }

            var destProp = destType.GetProperty(srcProp.Name, BindingFlags.Public | BindingFlags.Instance);
            if (destProp == null || !destProp.CanWrite)
            {
                continue;
            }

            if (!destProp.PropertyType.IsAssignableFrom(srcProp.PropertyType))
            {
                continue;
            }

            var value = srcProp.GetValue(source);
            if (IsValueDefault(value))
            {
                continue;
            }

            destProp.SetValue(destination, value);
        }
    }

    private static bool IsValueDefault(object? value)
    {
        if (value == null)
        {
            return true;
        }

        var type = value.GetType();
        if (!type.IsValueType)
        {
            return false;
        }

        return value.Equals(Activator.CreateInstance(type));
    }
}
