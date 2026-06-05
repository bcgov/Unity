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

[Mapper]
public partial class CreateUpdateDynamicUrlDtoToDynamicUrlMapper : MapperBase<CreateUpdateDynamicUrlDto, DynamicUrl>
{
    [MapperIgnoreTarget(nameof(DynamicUrl.Id))]
    [MapperIgnoreTarget(nameof(DynamicUrl.TenantId))]
    [MapperIgnoreTarget(nameof(DynamicUrl.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(DynamicUrl.IsDeleted))]
    [MapperIgnoreTarget(nameof(DynamicUrl.DeleterId))]
    [MapperIgnoreTarget(nameof(DynamicUrl.DeletionTime))]
    [MapperIgnoreTarget(nameof(DynamicUrl.LastModificationTime))]
    [MapperIgnoreTarget(nameof(DynamicUrl.LastModifierId))]
    [MapperIgnoreTarget(nameof(DynamicUrl.CreationTime))]
    [MapperIgnoreTarget(nameof(DynamicUrl.CreatorId))]
    public override partial DynamicUrl Map(CreateUpdateDynamicUrlDto source);

    [MapperIgnoreTarget(nameof(DynamicUrl.Id))]
    [MapperIgnoreTarget(nameof(DynamicUrl.TenantId))]
    [MapperIgnoreTarget(nameof(DynamicUrl.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(DynamicUrl.IsDeleted))]
    [MapperIgnoreTarget(nameof(DynamicUrl.DeleterId))]
    [MapperIgnoreTarget(nameof(DynamicUrl.DeletionTime))]
    [MapperIgnoreTarget(nameof(DynamicUrl.LastModificationTime))]
    [MapperIgnoreTarget(nameof(DynamicUrl.LastModifierId))]
    [MapperIgnoreTarget(nameof(DynamicUrl.CreationTime))]
    [MapperIgnoreTarget(nameof(DynamicUrl.CreatorId))]
    public override partial void Map(CreateUpdateDynamicUrlDto source, DynamicUrl destination);
}

[Mapper]
public partial class CreateUpdateDynamicUrlDtoToDynamicUrlDtoMapper : MapperBase<CreateUpdateDynamicUrlDto, DynamicUrlDto>
{
    [MapperIgnoreTarget(nameof(DynamicUrlDto.Id))]
    [MapperIgnoreTarget(nameof(DynamicUrlDto.TenantId))]
    [MapperIgnoreTarget(nameof(DynamicUrlDto.CreationTime))]
    [MapperIgnoreTarget(nameof(DynamicUrlDto.CreatorId))]
    [MapperIgnoreTarget(nameof(DynamicUrlDto.LastModificationTime))]
    [MapperIgnoreTarget(nameof(DynamicUrlDto.LastModifierId))]
    public override partial DynamicUrlDto Map(CreateUpdateDynamicUrlDto source);

    [MapperIgnoreTarget(nameof(DynamicUrlDto.Id))]
    [MapperIgnoreTarget(nameof(DynamicUrlDto.TenantId))]
    [MapperIgnoreTarget(nameof(DynamicUrlDto.CreationTime))]
    [MapperIgnoreTarget(nameof(DynamicUrlDto.CreatorId))]
    [MapperIgnoreTarget(nameof(DynamicUrlDto.LastModificationTime))]
    [MapperIgnoreTarget(nameof(DynamicUrlDto.LastModifierId))]
    public override partial void Map(CreateUpdateDynamicUrlDto source, DynamicUrlDto destination);
}

[Mapper]
public partial class DynamicUrlDtoToCreateUpdateDynamicUrlDtoMapper : MapperBase<DynamicUrlDto, CreateUpdateDynamicUrlDto> 
{ public override partial CreateUpdateDynamicUrlDto Map(DynamicUrlDto source); public override partial void Map(DynamicUrlDto source, CreateUpdateDynamicUrlDto destination); }

[Mapper] 
public partial class DynamicUrlToDynamicUrlDtoMapper : MapperBase<DynamicUrl, DynamicUrlDto> 
{ public override partial DynamicUrlDto Map(DynamicUrl source); public override partial void Map(DynamicUrl source, DynamicUrlDto destination); }

[Mapper] 
public partial class DynamicUrlToCreateUpdateDynamicUrlDtoMapper : MapperBase<DynamicUrl, CreateUpdateDynamicUrlDto> 
{ public override partial CreateUpdateDynamicUrlDto Map(DynamicUrl source); public override partial void Map(DynamicUrl source, CreateUpdateDynamicUrlDto destination); }

[Mapper(AllowNullPropertyAssignment = true)]
public partial class ApplicationToGrantApplicationDtoMapper : MapperBase<Application, GrantApplicationDto>
{
    [MapperIgnoreTarget(nameof(GrantApplicationDto.RowCount))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.Assignees))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.Status))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.Probability))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.ApplicationName))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.Category))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.EconomicRegion))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.City))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.TotalProjectBudget))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.Sector))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.SubSector))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.ProjectSummary))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.TotalScore))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.RecommendedAmount))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.ApprovedAmount))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.LikelihoodOfFunding))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.DueDiligenceStatus))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.SubStatus))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.SubStatusDisplayValue))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.DeclineRational))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.Notes))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.StatusCode))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.ContactFullName))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.ContactTitle))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.ContactEmail))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.ContactBusinessPhone))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.ContactCellPhone))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.ApplicationTag))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.OrganizationName))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.NonRegOrgName))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.OrganizationType))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.OrgStatus))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.BusinessNumber))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.OrganizationSize))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.OrgNumber))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.SectorSubSectorIndustryDesc))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.PaymentInfo))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.AIAnalysisData))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.Owner))]
    public override partial GrantApplicationDto Map(Application source);

    [MapperIgnoreTarget(nameof(GrantApplicationDto.RowCount))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.Assignees))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.Status))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.Probability))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.ApplicationName))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.Category))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.EconomicRegion))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.City))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.TotalProjectBudget))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.Sector))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.SubSector))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.ProjectSummary))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.TotalScore))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.RecommendedAmount))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.ApprovedAmount))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.LikelihoodOfFunding))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.DueDiligenceStatus))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.SubStatus))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.SubStatusDisplayValue))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.DeclineRational))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.Notes))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.StatusCode))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.ContactFullName))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.ContactTitle))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.ContactEmail))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.ContactBusinessPhone))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.ContactCellPhone))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.ApplicationTag))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.OrganizationName))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.NonRegOrgName))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.OrganizationType))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.OrgStatus))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.BusinessNumber))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.OrganizationSize))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.OrgNumber))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.SectorSubSectorIndustryDesc))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.PaymentInfo))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.AIAnalysisData))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.Owner))]
    public override partial void Map(Application source, GrantApplicationDto destination);

    [MapperIgnoreTarget(nameof(GrantApplicationApplicantDto.SiteId))]
    [MapperIgnoreTarget(nameof(GrantApplicationApplicantDto.ElectoralDistrict))]
    [MapPropertyFromSource(nameof(GrantApplicationApplicantDto.FiscalDay), Use = nameof(ResolveFiscalDay))]
    [MapPropertyFromSource(nameof(GrantApplicationApplicantDto.SupplierId), Use = nameof(ResolveApplicantSupplierId))]
    [MapPropertyFromSource(nameof(GrantApplicationApplicantDto.RedStop), Use = nameof(ResolveApplicantRedStop))]
    private partial GrantApplicationApplicantDto ToDto(Applicant source);

    [MapperIgnoreTarget(nameof(ApplicationFormDto.ChefsFormVersionGuid))]
    [MapperIgnoreTarget(nameof(ApplicationFormDto.SubmissionHeaderMapping))]
    [MapperIgnoreTarget(nameof(ApplicationFormDto.ApiToken))]
    private partial ApplicationFormDto ToDto(ApplicationForm source);

    private static string ResolveFiscalDay(Applicant src) => src.FiscalDay?.ToString() ?? string.Empty;
    private static Guid ResolveApplicantSupplierId(Applicant src) => src.SupplierId ?? Guid.Empty;
    private static bool ResolveApplicantRedStop(Applicant src) => src.RedStop ?? false;
}
[Mapper]
public partial class ApplicationAssignmentToGrantApplicationAssigneeDtoMapper : MapperBase<ApplicationAssignment, GrantApplicationAssigneeDto>
{
    [MapperIgnoreTarget(nameof(GrantApplicationAssigneeDto.FullName))]
    [MapperIgnoreTarget(nameof(GrantApplicationAssigneeDto.Email))]
    public override partial GrantApplicationAssigneeDto Map(ApplicationAssignment source);

    [MapperIgnoreTarget(nameof(GrantApplicationAssigneeDto.FullName))]
    [MapperIgnoreTarget(nameof(GrantApplicationAssigneeDto.Email))]
    public override partial void Map(ApplicationAssignment source, GrantApplicationAssigneeDto destination);
}

[Mapper]
public partial class PersonToGrantApplicationAssigneeDtoMapper : MapperBase<Person, GrantApplicationAssigneeDto>
{
    [MapProperty(nameof(Person.Id), nameof(GrantApplicationAssigneeDto.AssigneeId))]
    [MapperIgnoreTarget(nameof(GrantApplicationAssigneeDto.ApplicationId))]
    [MapperIgnoreTarget(nameof(GrantApplicationAssigneeDto.Duty))]
    [MapperIgnoreTarget(nameof(GrantApplicationAssigneeDto.Email))]
    public override partial GrantApplicationAssigneeDto Map(Person source);

    [MapProperty(nameof(Person.Id), nameof(GrantApplicationAssigneeDto.AssigneeId))]
    [MapperIgnoreTarget(nameof(GrantApplicationAssigneeDto.ApplicationId))]
    [MapperIgnoreTarget(nameof(GrantApplicationAssigneeDto.Duty))]
    [MapperIgnoreTarget(nameof(GrantApplicationAssigneeDto.Email))]
    public override partial void Map(Person source, GrantApplicationAssigneeDto destination);
}

[Mapper] public partial class ApplicationStatusToApplicationStatusDtoMapper : MapperBase<ApplicationStatus, ApplicationStatusDto> { public override partial ApplicationStatusDto Map(ApplicationStatus source); public override partial void Map(ApplicationStatus source, ApplicationStatusDto destination); }

[Mapper]
public partial class AssessmentCommentToCommentDtoMapper : MapperBase<AssessmentComment, CommentDto>
{
    [MapProperty(nameof(AssessmentComment.AssessmentId), nameof(CommentDto.OwnerId))]
    [MapperIgnoreTarget(nameof(CommentDto.Badge))]
    [MapperIgnoreTarget(nameof(CommentDto.Commenter))]
    public override partial CommentDto Map(AssessmentComment source);

    [MapProperty(nameof(AssessmentComment.AssessmentId), nameof(CommentDto.OwnerId))]
    [MapperIgnoreTarget(nameof(CommentDto.Badge))]
    [MapperIgnoreTarget(nameof(CommentDto.Commenter))]
    public override partial void Map(AssessmentComment source, CommentDto destination);
}

[Mapper]
public partial class ApplicationCommentToCommentDtoMapper : MapperBase<ApplicationComment, CommentDto>
{
    [MapProperty(nameof(ApplicationComment.ApplicationId), nameof(CommentDto.OwnerId))]
    [MapperIgnoreTarget(nameof(CommentDto.Badge))]
    [MapperIgnoreTarget(nameof(CommentDto.Commenter))]
    public override partial CommentDto Map(ApplicationComment source);

    [MapProperty(nameof(ApplicationComment.ApplicationId), nameof(CommentDto.OwnerId))]
    [MapperIgnoreTarget(nameof(CommentDto.Badge))]
    [MapperIgnoreTarget(nameof(CommentDto.Commenter))]
    public override partial void Map(ApplicationComment source, CommentDto destination);
}

[Mapper]
public partial class ApplicantCommentToCommentDtoMapper : MapperBase<ApplicantComment, CommentDto>
{
    [MapProperty(nameof(ApplicantComment.ApplicantId), nameof(CommentDto.OwnerId))]
    [MapperIgnoreTarget(nameof(CommentDto.Badge))]
    [MapperIgnoreTarget(nameof(CommentDto.Commenter))]
    public override partial CommentDto Map(ApplicantComment source);

    [MapProperty(nameof(ApplicantComment.ApplicantId), nameof(CommentDto.OwnerId))]
    [MapperIgnoreTarget(nameof(CommentDto.Badge))]
    [MapperIgnoreTarget(nameof(CommentDto.Commenter))]
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
    [MapperIgnoreTarget(nameof(ApplicationChefsFileAttachmentDto.Name))]
    public override partial ApplicationChefsFileAttachmentDto Map(ApplicationChefsFileAttachment source);

    [MapProperty(nameof(ApplicationChefsFileAttachment.CreationTime), nameof(ApplicationChefsFileAttachmentDto.CreatedTime))]
    [MapProperty(nameof(ApplicationChefsFileAttachment.LastModificationTime), nameof(ApplicationChefsFileAttachmentDto.UpdatedTime))]
    [MapperIgnoreTarget(nameof(ApplicationChefsFileAttachmentDto.Name))]
    public override partial void Map(ApplicationChefsFileAttachment source, ApplicationChefsFileAttachmentDto destination);
}

[Mapper]
public partial class ApplicationAttachmentToDtoMapper : MapperBase<ApplicationAttachment, ApplicationAttachmentDto>
{
    [MapperIgnoreTarget(nameof(ApplicationAttachmentDto.AttachedBy))]
    public override partial ApplicationAttachmentDto Map(ApplicationAttachment source);

    [MapperIgnoreTarget(nameof(ApplicationAttachmentDto.AttachedBy))]
    public override partial void Map(ApplicationAttachment source, ApplicationAttachmentDto destination);
}
[Mapper] public partial class IntakeToIntakeDtoMapper : MapperBase<Intakes.Intake, IntakeDto> { public override partial IntakeDto Map(Intakes.Intake source); public override partial void Map(Intakes.Intake source, IntakeDto destination); }
[Mapper]
public partial class ApplicationFormToDtoMapper : MapperBase<ApplicationForm, ApplicationFormDto>
{
    [MapperIgnoreTarget(nameof(ApplicationFormDto.ChefsFormVersionGuid))]
    [MapperIgnoreTarget(nameof(ApplicationFormDto.SubmissionHeaderMapping))]
    [MapperIgnoreTarget(nameof(ApplicationFormDto.ApiToken))]
    public override partial ApplicationFormDto Map(ApplicationForm source);

    [MapperIgnoreTarget(nameof(ApplicationFormDto.ChefsFormVersionGuid))]
    [MapperIgnoreTarget(nameof(ApplicationFormDto.SubmissionHeaderMapping))]
    [MapperIgnoreTarget(nameof(ApplicationFormDto.ApiToken))]
    public override partial void Map(ApplicationForm source, ApplicationFormDto destination);
}
[Mapper]
public partial class ApplicationFormDtoToEntityMapper : MapperBase<ApplicationFormDto, ApplicationForm>
{
    [MapperIgnoreTarget(nameof(ApplicationForm.DeleterId))]
    [MapperIgnoreTarget(nameof(ApplicationForm.DeletionTime))]
    [MapperIgnoreTarget(nameof(ApplicationForm.LastModificationTime))]
    [MapperIgnoreTarget(nameof(ApplicationForm.LastModifierId))]
    [MapperIgnoreTarget(nameof(ApplicationForm.CreationTime))]
    [MapperIgnoreTarget(nameof(ApplicationForm.CreatorId))]
    [MapperIgnoreTarget(nameof(ApplicationForm.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(ApplicationForm.FormHierarchy))]
    [MapperIgnoreTarget(nameof(ApplicationForm.ParentFormId))]
    [MapperIgnoreTarget(nameof(ApplicationForm.IsDeleted))]
    [MapperIgnoreTarget(nameof(ApplicationForm.PaymentApprovalThreshold))]
    public override partial ApplicationForm Map(ApplicationFormDto source);

    [MapperIgnoreTarget(nameof(ApplicationForm.DeleterId))]
    [MapperIgnoreTarget(nameof(ApplicationForm.DeletionTime))]
    [MapperIgnoreTarget(nameof(ApplicationForm.LastModificationTime))]
    [MapperIgnoreTarget(nameof(ApplicationForm.LastModifierId))]
    [MapperIgnoreTarget(nameof(ApplicationForm.CreationTime))]
    [MapperIgnoreTarget(nameof(ApplicationForm.CreatorId))]
    [MapperIgnoreTarget(nameof(ApplicationForm.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(ApplicationForm.FormHierarchy))]
    [MapperIgnoreTarget(nameof(ApplicationForm.ParentFormId))]
    [MapperIgnoreTarget(nameof(ApplicationForm.IsDeleted))]
    [MapperIgnoreTarget(nameof(ApplicationForm.PaymentApprovalThreshold))]
    public override partial void Map(ApplicationFormDto source, ApplicationForm destination);
}
[Mapper] public partial class ApplicationFormVersionToDtoMapper : MapperBase<ApplicationFormVersion, ApplicationFormVersionDto> { public override partial ApplicationFormVersionDto Map(ApplicationFormVersion source); public override partial void Map(ApplicationFormVersion source, ApplicationFormVersionDto destination); }
[Mapper]
public partial class ApplicationFormVersionDtoToEntityMapper : MapperBase<ApplicationFormVersionDto, ApplicationFormVersion>
{
    [MapperIgnoreTarget(nameof(ApplicationFormVersion.TenantId))]
    [MapperIgnoreTarget(nameof(ApplicationFormVersion.LastModificationTime))]
    [MapperIgnoreTarget(nameof(ApplicationFormVersion.LastModifierId))]
    [MapperIgnoreTarget(nameof(ApplicationFormVersion.CreationTime))]
    [MapperIgnoreTarget(nameof(ApplicationFormVersion.CreatorId))]
    [MapperIgnoreTarget(nameof(ApplicationFormVersion.ConcurrencyStamp))]
    public override partial ApplicationFormVersion Map(ApplicationFormVersionDto source);

    [MapperIgnoreTarget(nameof(ApplicationFormVersion.TenantId))]
    [MapperIgnoreTarget(nameof(ApplicationFormVersion.LastModificationTime))]
    [MapperIgnoreTarget(nameof(ApplicationFormVersion.LastModifierId))]
    [MapperIgnoreTarget(nameof(ApplicationFormVersion.CreationTime))]
    [MapperIgnoreTarget(nameof(ApplicationFormVersion.CreatorId))]
    [MapperIgnoreTarget(nameof(ApplicationFormVersion.ConcurrencyStamp))]
    public override partial void Map(ApplicationFormVersionDto source, ApplicationFormVersion destination);
}
[Mapper]
public partial class CreateUpdateApplicationFormVersionDtoToEntityMapper : MapperBase<CreateUpdateApplicationFormVersionDto, ApplicationFormVersion>
{
    [MapperIgnoreTarget(nameof(ApplicationFormVersion.TenantId))]
    [MapperIgnoreTarget(nameof(ApplicationFormVersion.ReportColumns))]
    [MapperIgnoreTarget(nameof(ApplicationFormVersion.ReportKeys))]
    [MapperIgnoreTarget(nameof(ApplicationFormVersion.ReportViewName))]
    [MapperIgnoreTarget(nameof(ApplicationFormVersion.FormSchema))]
    [MapperIgnoreTarget(nameof(ApplicationFormVersion.LastModificationTime))]
    [MapperIgnoreTarget(nameof(ApplicationFormVersion.LastModifierId))]
    [MapperIgnoreTarget(nameof(ApplicationFormVersion.CreationTime))]
    [MapperIgnoreTarget(nameof(ApplicationFormVersion.CreatorId))]
    [MapperIgnoreTarget(nameof(ApplicationFormVersion.ConcurrencyStamp))]
    public override partial ApplicationFormVersion Map(CreateUpdateApplicationFormVersionDto source);

    [MapperIgnoreTarget(nameof(ApplicationFormVersion.TenantId))]
    [MapperIgnoreTarget(nameof(ApplicationFormVersion.ReportColumns))]
    [MapperIgnoreTarget(nameof(ApplicationFormVersion.ReportKeys))]
    [MapperIgnoreTarget(nameof(ApplicationFormVersion.ReportViewName))]
    [MapperIgnoreTarget(nameof(ApplicationFormVersion.FormSchema))]
    [MapperIgnoreTarget(nameof(ApplicationFormVersion.LastModificationTime))]
    [MapperIgnoreTarget(nameof(ApplicationFormVersion.LastModifierId))]
    [MapperIgnoreTarget(nameof(ApplicationFormVersion.CreationTime))]
    [MapperIgnoreTarget(nameof(ApplicationFormVersion.CreatorId))]
    [MapperIgnoreTarget(nameof(ApplicationFormVersion.ConcurrencyStamp))]
    public override partial void Map(CreateUpdateApplicationFormVersionDto source, ApplicationFormVersion destination);
}
[Mapper]
public partial class CreateUpdateIntakeDtoToEntityMapper : MapperBase<CreateUpdateIntakeDto, Intakes.Intake>
{
    [MapperIgnoreTarget(nameof(Intakes.Intake.TenantId))]
    [MapperIgnoreTarget(nameof(Intakes.Intake.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(Intakes.Intake.IsDeleted))]
    [MapperIgnoreTarget(nameof(Intakes.Intake.DeleterId))]
    [MapperIgnoreTarget(nameof(Intakes.Intake.DeletionTime))]
    [MapperIgnoreTarget(nameof(Intakes.Intake.LastModificationTime))]
    [MapperIgnoreTarget(nameof(Intakes.Intake.LastModifierId))]
    [MapperIgnoreTarget(nameof(Intakes.Intake.CreationTime))]
    [MapperIgnoreTarget(nameof(Intakes.Intake.CreatorId))]
    public override partial Intakes.Intake Map(CreateUpdateIntakeDto source);

    [MapperIgnoreTarget(nameof(Intakes.Intake.TenantId))]
    [MapperIgnoreTarget(nameof(Intakes.Intake.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(Intakes.Intake.IsDeleted))]
    [MapperIgnoreTarget(nameof(Intakes.Intake.DeleterId))]
    [MapperIgnoreTarget(nameof(Intakes.Intake.DeletionTime))]
    [MapperIgnoreTarget(nameof(Intakes.Intake.LastModificationTime))]
    [MapperIgnoreTarget(nameof(Intakes.Intake.LastModifierId))]
    [MapperIgnoreTarget(nameof(Intakes.Intake.CreationTime))]
    [MapperIgnoreTarget(nameof(Intakes.Intake.CreatorId))]
    public override partial void Map(CreateUpdateIntakeDto source, Intakes.Intake destination);
}
[Mapper]
public partial class CreateUpdateApplicationFormDtoToEntityMapper : MapperBase<CreateUpdateApplicationFormDto, ApplicationForm>
{
    [MapperIgnoreTarget(nameof(ApplicationForm.AvailableChefsFields))]
    [MapperIgnoreTarget(nameof(ApplicationForm.ConnectionHttpStatus))]
    [MapperIgnoreTarget(nameof(ApplicationForm.AttemptedConnectionDate))]
    [MapperIgnoreTarget(nameof(ApplicationForm.PreventPayment))]
    [MapperIgnoreTarget(nameof(ApplicationForm.AccountCodingId))]
    [MapperIgnoreTarget(nameof(ApplicationForm.ScoresheetId))]
    [MapperIgnoreTarget(nameof(ApplicationForm.TenantId))]
    [MapperIgnoreTarget(nameof(ApplicationForm.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(ApplicationForm.IsDeleted))]
    [MapperIgnoreTarget(nameof(ApplicationForm.DeleterId))]
    [MapperIgnoreTarget(nameof(ApplicationForm.DeletionTime))]
    [MapperIgnoreTarget(nameof(ApplicationForm.LastModificationTime))]
    [MapperIgnoreTarget(nameof(ApplicationForm.LastModifierId))]
    [MapperIgnoreTarget(nameof(ApplicationForm.CreationTime))]
    [MapperIgnoreTarget(nameof(ApplicationForm.CreatorId))]
    [MapperIgnoreTarget(nameof(ApplicationForm.PaymentApprovalThreshold))]
    [MapperIgnoreTarget(nameof(ApplicationForm.DefaultPaymentGroup))]
    [MapperIgnoreTarget(nameof(ApplicationForm.FormHierarchy))]
    [MapperIgnoreTarget(nameof(ApplicationForm.ParentFormId))]
    [MapperIgnoreTarget(nameof(ApplicationForm.IsDirectApproval))]
    [MapperIgnoreTarget(nameof(ApplicationForm.AutomaticallyGenerateAIAnalysis))]
    [MapperIgnoreTarget(nameof(ApplicationForm.ManuallyInitiateAIAnalysis))]
    [MapperIgnoreTarget(nameof(ApplicationForm.Prefix))]
    [MapperIgnoreTarget(nameof(ApplicationForm.SuffixType))]
    [MapperIgnoreTarget(nameof(ApplicationForm.ElectoralDistrictAddressType))]
    public override partial ApplicationForm Map(CreateUpdateApplicationFormDto source);

    [MapperIgnoreTarget(nameof(ApplicationForm.AvailableChefsFields))]
    [MapperIgnoreTarget(nameof(ApplicationForm.ConnectionHttpStatus))]
    [MapperIgnoreTarget(nameof(ApplicationForm.AttemptedConnectionDate))]
    [MapperIgnoreTarget(nameof(ApplicationForm.PreventPayment))]
    [MapperIgnoreTarget(nameof(ApplicationForm.AccountCodingId))]
    [MapperIgnoreTarget(nameof(ApplicationForm.ScoresheetId))]
    [MapperIgnoreTarget(nameof(ApplicationForm.TenantId))]
    [MapperIgnoreTarget(nameof(ApplicationForm.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(ApplicationForm.IsDeleted))]
    [MapperIgnoreTarget(nameof(ApplicationForm.DeleterId))]
    [MapperIgnoreTarget(nameof(ApplicationForm.DeletionTime))]
    [MapperIgnoreTarget(nameof(ApplicationForm.LastModificationTime))]
    [MapperIgnoreTarget(nameof(ApplicationForm.LastModifierId))]
    [MapperIgnoreTarget(nameof(ApplicationForm.CreationTime))]
    [MapperIgnoreTarget(nameof(ApplicationForm.CreatorId))]
    [MapperIgnoreTarget(nameof(ApplicationForm.PaymentApprovalThreshold))]
    [MapperIgnoreTarget(nameof(ApplicationForm.DefaultPaymentGroup))]
    [MapperIgnoreTarget(nameof(ApplicationForm.FormHierarchy))]
    [MapperIgnoreTarget(nameof(ApplicationForm.ParentFormId))]
    [MapperIgnoreTarget(nameof(ApplicationForm.IsDirectApproval))]
    [MapperIgnoreTarget(nameof(ApplicationForm.AutomaticallyGenerateAIAnalysis))]
    [MapperIgnoreTarget(nameof(ApplicationForm.ManuallyInitiateAIAnalysis))]
    [MapperIgnoreTarget(nameof(ApplicationForm.Prefix))]
    [MapperIgnoreTarget(nameof(ApplicationForm.SuffixType))]
    [MapperIgnoreTarget(nameof(ApplicationForm.ElectoralDistrictAddressType))]
    public override partial void Map(CreateUpdateApplicationFormDto source, ApplicationForm destination);
}
[Mapper]
public partial class AssessmentAttachmentToDtoMapper : MapperBase<AssessmentAttachment, AssessmentAttachmentDto>
{
    [MapperIgnoreTarget(nameof(AssessmentAttachmentDto.AttachedBy))]
    public override partial AssessmentAttachmentDto Map(AssessmentAttachment source);

    [MapperIgnoreTarget(nameof(AssessmentAttachmentDto.AttachedBy))]
    public override partial void Map(AssessmentAttachment source, AssessmentAttachmentDto destination);
}

[Mapper]
public partial class ApplicationActionResultItemToDtoMapper : MapperBase<ApplicationActionResultItem, ApplicationActionDto>
{
    [MapperIgnoreTarget(nameof(ApplicationActionDto.IsAuthorized))]
    public override partial ApplicationActionDto Map(ApplicationActionResultItem source);

    [MapperIgnoreTarget(nameof(ApplicationActionDto.IsAuthorized))]
    public override partial void Map(ApplicationActionResultItem source, ApplicationActionDto destination);
}
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
[Mapper(AllowNullPropertyAssignment = true)]
public partial class ApplicantToGrantApplicationApplicantDtoMapper : MapperBase<Applicant, GrantApplicationApplicantDto>
{
    [MapProperty(nameof(Applicant.Id), nameof(GrantApplicationApplicantDto.Id))]
    [MapperIgnoreTarget(nameof(GrantApplicationApplicantDto.SiteId))]
    [MapperIgnoreTarget(nameof(GrantApplicationApplicantDto.ElectoralDistrict))]
    [MapPropertyFromSource(nameof(GrantApplicationApplicantDto.FiscalDay), Use = nameof(ResolveFiscalDay))]
    [MapPropertyFromSource(nameof(GrantApplicationApplicantDto.SupplierId), Use = nameof(ResolveSupplierId))]
    [MapPropertyFromSource(nameof(GrantApplicationApplicantDto.RedStop), Use = nameof(ResolveRedStop))]
    public override partial GrantApplicationApplicantDto Map(Applicant source);

    [MapProperty(nameof(Applicant.Id), nameof(GrantApplicationApplicantDto.Id))]
    [MapperIgnoreTarget(nameof(GrantApplicationApplicantDto.SiteId))]
    [MapperIgnoreTarget(nameof(GrantApplicationApplicantDto.ElectoralDistrict))]
    [MapPropertyFromSource(nameof(GrantApplicationApplicantDto.FiscalDay), Use = nameof(ResolveFiscalDay))]
    [MapPropertyFromSource(nameof(GrantApplicationApplicantDto.SupplierId), Use = nameof(ResolveSupplierId))]
    [MapPropertyFromSource(nameof(GrantApplicationApplicantDto.RedStop), Use = nameof(ResolveRedStop))]
    public override partial void Map(Applicant source, GrantApplicationApplicantDto destination);

    private static string ResolveFiscalDay(Applicant src) => src.FiscalDay?.ToString() ?? string.Empty;
    private static Guid ResolveSupplierId(Applicant src) => src.SupplierId ?? Guid.Empty;
    private static bool ResolveRedStop(Applicant src) => src.RedStop ?? false;
}
[Mapper] public partial class ApplicationContactToDtoMapper : MapperBase<ApplicationContact, ApplicationContactDto> { public override partial ApplicationContactDto Map(ApplicationContact source); public override partial void Map(ApplicationContact source, ApplicationContactDto destination); }
[Mapper]
public partial class ApplicationContactDtoToEntityMapper : MapperBase<ApplicationContactDto, ApplicationContact>
{
    [MapperIgnoreTarget(nameof(ApplicationContact.TenantId))]
    [MapperIgnoreTarget(nameof(ApplicationContact.CreationTime))]
    [MapperIgnoreTarget(nameof(ApplicationContact.CreatorId))]
    [MapperIgnoreTarget(nameof(ApplicationContact.LastModificationTime))]
    [MapperIgnoreTarget(nameof(ApplicationContact.LastModifierId))]
    [MapperIgnoreTarget(nameof(ApplicationContact.ConcurrencyStamp))]
    public override partial ApplicationContact Map(ApplicationContactDto source);

    [MapperIgnoreTarget(nameof(ApplicationContact.TenantId))]
    [MapperIgnoreTarget(nameof(ApplicationContact.CreationTime))]
    [MapperIgnoreTarget(nameof(ApplicationContact.CreatorId))]
    [MapperIgnoreTarget(nameof(ApplicationContact.LastModificationTime))]
    [MapperIgnoreTarget(nameof(ApplicationContact.LastModifierId))]
    [MapperIgnoreTarget(nameof(ApplicationContact.ConcurrencyStamp))]
    public override partial void Map(ApplicationContactDto source, ApplicationContact destination);
}
[Mapper] public partial class ApplicationLinkToDtoMapper : MapperBase<ApplicationLink, ApplicationLinksDto> { public override partial ApplicationLinksDto Map(ApplicationLink source); public override partial void Map(ApplicationLink source, ApplicationLinksDto destination); }
[Mapper]
public partial class ApplicationLinksDtoToEntityMapper : MapperBase<ApplicationLinksDto, ApplicationLink>
{
    [MapperIgnoreTarget(nameof(ApplicationLink.TenantId))]
    [MapperIgnoreTarget(nameof(ApplicationLink.CreationTime))]
    [MapperIgnoreTarget(nameof(ApplicationLink.CreatorId))]
    [MapperIgnoreTarget(nameof(ApplicationLink.LastModificationTime))]
    [MapperIgnoreTarget(nameof(ApplicationLink.LastModifierId))]
    [MapperIgnoreTarget(nameof(ApplicationLink.ConcurrencyStamp))]
    public override partial ApplicationLink Map(ApplicationLinksDto source);

    [MapperIgnoreTarget(nameof(ApplicationLink.TenantId))]
    [MapperIgnoreTarget(nameof(ApplicationLink.CreationTime))]
    [MapperIgnoreTarget(nameof(ApplicationLink.CreatorId))]
    [MapperIgnoreTarget(nameof(ApplicationLink.LastModificationTime))]
    [MapperIgnoreTarget(nameof(ApplicationLink.LastModifierId))]
    [MapperIgnoreTarget(nameof(ApplicationLink.ConcurrencyStamp))]
    public override partial void Map(ApplicationLinksDto source, ApplicationLink destination);
}
[Mapper(AllowNullPropertyAssignment = true)]
public partial class ApplicationToGrantApplicationLiteDtoMapper : MapperBase<Application, GrantApplicationLiteDto>
{
    [MapperIgnoreTarget(nameof(GrantApplicationLiteDto.ApplicantName))]
    [MapperIgnoreTarget(nameof(GrantApplicationLiteDto.OrganizationName))]
    [MapperIgnoreTarget(nameof(GrantApplicationLiteDto.UnityApplicantId))]
    public override partial GrantApplicationLiteDto Map(Application source);

    [MapperIgnoreTarget(nameof(GrantApplicationLiteDto.ApplicantName))]
    [MapperIgnoreTarget(nameof(GrantApplicationLiteDto.OrganizationName))]
    [MapperIgnoreTarget(nameof(GrantApplicationLiteDto.UnityApplicantId))]
    public override partial void Map(Application source, GrantApplicationLiteDto destination);
}
[Mapper(AllowNullPropertyAssignment = true)]
public partial class ApplicantAddressToDtoMapper : MapperBase<ApplicantAddress, ApplicantAddressDto>
{
    public override partial ApplicantAddressDto Map(ApplicantAddress source);
    public override partial void Map(ApplicantAddress source, ApplicantAddressDto destination);
}
[Mapper] public partial class AccountCodingToAccountCodingDtoGmMapper : MapperBase<AccountCoding, AccountCodingDto> { public override partial AccountCodingDto Map(AccountCoding source); public override partial void Map(AccountCoding source, AccountCodingDto destination); }
[Mapper] public partial class TagToDtoMapper : MapperBase<Tag, TagDto> { public override partial TagDto Map(Tag source); public override partial void Map(Tag source, TagDto destination); }
[Mapper] public partial class TagSummaryCountToDtoGmMapper : MapperBase<TagSummaryCount, TagSummaryCountDto> { public override partial TagSummaryCountDto Map(TagSummaryCount source); public override partial void Map(TagSummaryCount source, TagSummaryCountDto destination); }
[Mapper] public partial class TagUsageSummaryToDtoMapper : MapperBase<TagUsageSummary, TagUsageSummaryDto> { public override partial TagUsageSummaryDto Map(TagUsageSummary source); public override partial void Map(TagUsageSummary source, TagUsageSummaryDto destination); }

[Mapper]
public partial class FundingHistoryToDtoMapper : MapperBase<FundingHistory, FundingHistoryDto>
{
    public override partial FundingHistoryDto Map(FundingHistory source);
    public override partial void Map(FundingHistory source, FundingHistoryDto destination);
}
[Mapper]
public partial class CreateUpdateFundingHistoryDtoToEntityMapper : MapperBase<CreateUpdateFundingHistoryDto, FundingHistory>
{
    [MapperIgnoreTarget(nameof(FundingHistory.Id))]
    [MapperIgnoreTarget(nameof(FundingHistory.TenantId))]
    [MapperIgnoreTarget(nameof(FundingHistory.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(FundingHistory.CreationTime))]
    [MapperIgnoreTarget(nameof(FundingHistory.CreatorId))]
    [MapperIgnoreTarget(nameof(FundingHistory.LastModificationTime))]
    [MapperIgnoreTarget(nameof(FundingHistory.LastModifierId))]
    public override partial FundingHistory Map(CreateUpdateFundingHistoryDto source);

    [MapperIgnoreTarget(nameof(FundingHistory.Id))]
    [MapperIgnoreTarget(nameof(FundingHistory.TenantId))]
    [MapperIgnoreTarget(nameof(FundingHistory.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(FundingHistory.CreationTime))]
    [MapperIgnoreTarget(nameof(FundingHistory.CreatorId))]
    [MapperIgnoreTarget(nameof(FundingHistory.LastModificationTime))]
    [MapperIgnoreTarget(nameof(FundingHistory.LastModifierId))]
    public override partial void Map(CreateUpdateFundingHistoryDto source, FundingHistory destination);
}

[Mapper]
public partial class FundingHistoryDtoToEntityMapper : MapperBase<FundingHistoryDto, FundingHistory>
{
    [MapperIgnoreTarget(nameof(FundingHistory.TenantId))]
    [MapperIgnoreTarget(nameof(FundingHistory.ConcurrencyStamp))]
    public override partial FundingHistory Map(FundingHistoryDto source);

    [MapperIgnoreTarget(nameof(FundingHistory.TenantId))]
    [MapperIgnoreTarget(nameof(FundingHistory.ConcurrencyStamp))]
    public override partial void Map(FundingHistoryDto source, FundingHistory destination);
}

[Mapper] public partial class IssueTrackingToDtoMapper : MapperBase<IssueTracking, IssueTrackingDto> { public override partial IssueTrackingDto Map(IssueTracking source); public override partial void Map(IssueTracking source, IssueTrackingDto destination); }
[Mapper]
public partial class CreateUpdateIssueTrackingDtoToEntityMapper : MapperBase<CreateUpdateIssueTrackingDto, IssueTracking>
{
    [MapperIgnoreTarget(nameof(IssueTracking.Id))]
    [MapperIgnoreTarget(nameof(IssueTracking.TenantId))]
    [MapperIgnoreTarget(nameof(IssueTracking.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(IssueTracking.CreationTime))]
    [MapperIgnoreTarget(nameof(IssueTracking.CreatorId))]
    [MapperIgnoreTarget(nameof(IssueTracking.LastModificationTime))]
    [MapperIgnoreTarget(nameof(IssueTracking.LastModifierId))]
    public override partial IssueTracking Map(CreateUpdateIssueTrackingDto source);

    [MapperIgnoreTarget(nameof(IssueTracking.Id))]
    [MapperIgnoreTarget(nameof(IssueTracking.TenantId))]
    [MapperIgnoreTarget(nameof(IssueTracking.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(IssueTracking.CreationTime))]
    [MapperIgnoreTarget(nameof(IssueTracking.CreatorId))]
    [MapperIgnoreTarget(nameof(IssueTracking.LastModificationTime))]
    [MapperIgnoreTarget(nameof(IssueTracking.LastModifierId))]
    public override partial void Map(CreateUpdateIssueTrackingDto source, IssueTracking destination);
}

[Mapper]
public partial class IssueTrackingDtoToEntityMapper : MapperBase<IssueTrackingDto, IssueTracking>
{
    [MapperIgnoreTarget(nameof(IssueTracking.TenantId))]
    [MapperIgnoreTarget(nameof(IssueTracking.ConcurrencyStamp))]
    public override partial IssueTracking Map(IssueTrackingDto source);

    [MapperIgnoreTarget(nameof(IssueTracking.TenantId))]
    [MapperIgnoreTarget(nameof(IssueTracking.ConcurrencyStamp))]
    public override partial void Map(IssueTrackingDto source, IssueTracking destination);
}

[Mapper] public partial class AuditHistoryToDtoMapper : MapperBase<AuditHistory, AuditHistoryDto> { public override partial AuditHistoryDto Map(AuditHistory source); public override partial void Map(AuditHistory source, AuditHistoryDto destination); }
[Mapper]
public partial class CreateUpdateAuditHistoryDtoToEntityMapper : MapperBase<CreateUpdateAuditHistoryDto, AuditHistory>
{
    [MapperIgnoreTarget(nameof(AuditHistory.Id))]
    [MapperIgnoreTarget(nameof(AuditHistory.TenantId))]
    [MapperIgnoreTarget(nameof(AuditHistory.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(AuditHistory.CreationTime))]
    [MapperIgnoreTarget(nameof(AuditHistory.CreatorId))]
    [MapperIgnoreTarget(nameof(AuditHistory.LastModificationTime))]
    [MapperIgnoreTarget(nameof(AuditHistory.LastModifierId))]
    public override partial AuditHistory Map(CreateUpdateAuditHistoryDto source);

    [MapperIgnoreTarget(nameof(AuditHistory.Id))]
    [MapperIgnoreTarget(nameof(AuditHistory.TenantId))]
    [MapperIgnoreTarget(nameof(AuditHistory.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(AuditHistory.CreationTime))]
    [MapperIgnoreTarget(nameof(AuditHistory.CreatorId))]
    [MapperIgnoreTarget(nameof(AuditHistory.LastModificationTime))]
    [MapperIgnoreTarget(nameof(AuditHistory.LastModifierId))]
    public override partial void Map(CreateUpdateAuditHistoryDto source, AuditHistory destination);
}

[Mapper]
public partial class AuditHistoryDtoToEntityMapper : MapperBase<AuditHistoryDto, AuditHistory>
{
    [MapperIgnoreTarget(nameof(AuditHistory.TenantId))]
    [MapperIgnoreTarget(nameof(AuditHistory.ConcurrencyStamp))]
    public override partial AuditHistory Map(AuditHistoryDto source);

    [MapperIgnoreTarget(nameof(AuditHistory.TenantId))]
    [MapperIgnoreTarget(nameof(AuditHistory.ConcurrencyStamp))]
    public override partial void Map(AuditHistoryDto source, AuditHistory destination);
}

[Mapper] public partial class ReportsHistoryToDtoMapper : MapperBase<ReportsHistory, ReportsHistoryDto> { public override partial ReportsHistoryDto Map(ReportsHistory source); public override partial void Map(ReportsHistory source, ReportsHistoryDto destination); }
[Mapper]
public partial class CreateUpdateReportsHistoryDtoToEntityMapper : MapperBase<CreateUpdateReportsHistoryDto, ReportsHistory>
{
    [MapperIgnoreTarget(nameof(ReportsHistory.Id))]
    [MapperIgnoreTarget(nameof(ReportsHistory.TenantId))]
    [MapperIgnoreTarget(nameof(ReportsHistory.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(ReportsHistory.CreationTime))]
    [MapperIgnoreTarget(nameof(ReportsHistory.CreatorId))]
    [MapperIgnoreTarget(nameof(ReportsHistory.LastModificationTime))]
    [MapperIgnoreTarget(nameof(ReportsHistory.LastModifierId))]
    public override partial ReportsHistory Map(CreateUpdateReportsHistoryDto source);

    [MapperIgnoreTarget(nameof(ReportsHistory.Id))]
    [MapperIgnoreTarget(nameof(ReportsHistory.TenantId))]
    [MapperIgnoreTarget(nameof(ReportsHistory.ConcurrencyStamp))]
    [MapperIgnoreTarget(nameof(ReportsHistory.CreationTime))]
    [MapperIgnoreTarget(nameof(ReportsHistory.CreatorId))]
    [MapperIgnoreTarget(nameof(ReportsHistory.LastModificationTime))]
    [MapperIgnoreTarget(nameof(ReportsHistory.LastModifierId))]
    public override partial void Map(CreateUpdateReportsHistoryDto source, ReportsHistory destination);
}

[Mapper]
public partial class ReportsHistoryDtoToEntityMapper : MapperBase<ReportsHistoryDto, ReportsHistory>
{
    [MapperIgnoreTarget(nameof(ReportsHistory.TenantId))]
    [MapperIgnoreTarget(nameof(ReportsHistory.ConcurrencyStamp))]
    public override partial ReportsHistory Map(ReportsHistoryDto source);

    [MapperIgnoreTarget(nameof(ReportsHistory.TenantId))]
    [MapperIgnoreTarget(nameof(ReportsHistory.ConcurrencyStamp))]
    public override partial void Map(ReportsHistoryDto source, ReportsHistory destination);
}

[Mapper]
public partial class ApplicationToApplicantInfoDtoMapper : MapperBase<Application, ApplicantInfoDto>
{
    [MapperIgnoreTarget(nameof(ApplicantInfoDto.ApplicantName))]
    [MapperIgnoreTarget(nameof(ApplicantInfoDto.ApplicationStatusCode))]
    [MapperIgnoreTarget(nameof(ApplicantInfoDto.ApplicantSummary))]
    [MapperIgnoreTarget(nameof(ApplicantInfoDto.SigningAuthority))]
    [MapperIgnoreTarget(nameof(ApplicantInfoDto.ContactInfo))]
    [MapperIgnoreTarget(nameof(ApplicantInfoDto.ApplicationId))]
    [MapperIgnoreTarget(nameof(ApplicantInfoDto.ApplicationReferenceNo))]
    [MapperIgnoreTarget(nameof(ApplicantInfoDto.CustomFields))]
    [MapperIgnoreTarget(nameof(ApplicantInfoDto.CorrelationId))]
    [MapperIgnoreTarget(nameof(ApplicantInfoDto.WorksheetId))]
    [MapperIgnoreTarget(nameof(ApplicantInfoDto.WorksheetIds))]
    public override partial ApplicantInfoDto Map(Application source);

    [MapperIgnoreTarget(nameof(ApplicantInfoDto.ApplicantName))]
    [MapperIgnoreTarget(nameof(ApplicantInfoDto.ApplicationStatusCode))]
    [MapperIgnoreTarget(nameof(ApplicantInfoDto.ApplicantSummary))]
    [MapperIgnoreTarget(nameof(ApplicantInfoDto.SigningAuthority))]
    [MapperIgnoreTarget(nameof(ApplicantInfoDto.ContactInfo))]
    [MapperIgnoreTarget(nameof(ApplicantInfoDto.ApplicationId))]
    [MapperIgnoreTarget(nameof(ApplicantInfoDto.ApplicationReferenceNo))]
    [MapperIgnoreTarget(nameof(ApplicantInfoDto.CustomFields))]
    [MapperIgnoreTarget(nameof(ApplicantInfoDto.CorrelationId))]
    [MapperIgnoreTarget(nameof(ApplicantInfoDto.WorksheetId))]
    [MapperIgnoreTarget(nameof(ApplicantInfoDto.WorksheetIds))]
    public override partial void Map(Application source, ApplicantInfoDto destination);
}

[Mapper]
public partial class ApplicationToSigningAuthorityDtoMapper : MapperBase<Application, SigningAuthorityDto>
{
    public override partial SigningAuthorityDto Map(Application source);
    public override partial void Map(Application source, SigningAuthorityDto destination);
}

[Mapper]
public partial class ApplicantAgentToContactInfoDtoMapper : MapperBase<ApplicantAgent, ContactInfoDto>
{
    [MapProperty("Id", nameof(ContactInfoDto.ApplicantAgentId))]
    public override partial ContactInfoDto Map(ApplicantAgent source);

    [MapProperty("Id", nameof(ContactInfoDto.ApplicantAgentId))]
    public override partial void Map(ApplicantAgent source, ContactInfoDto destination);
}

[Mapper]
public partial class ApplicantToApplicantSummaryDtoMapper : MapperBase<Applicant, ApplicantSummaryDto>
{
    [MapProperty("Id", nameof(ApplicantSummaryDto.ApplicantId))]
    [MapperIgnoreTarget(nameof(ApplicantSummaryDto.ElectoralDistrict))]
    [MapPropertyFromSource(nameof(ApplicantSummaryDto.FiscalDay), Use = nameof(ResolveFiscalDay))]
    public override partial ApplicantSummaryDto Map(Applicant source);

    [MapProperty("Id", nameof(ApplicantSummaryDto.ApplicantId))]
    [MapperIgnoreTarget(nameof(ApplicantSummaryDto.ElectoralDistrict))]
    [MapPropertyFromSource(nameof(ApplicantSummaryDto.FiscalDay), Use = nameof(ResolveFiscalDay))]
    public override partial void Map(Applicant source, ApplicantSummaryDto destination);

    private static bool? MapIndigenousOrgInd(string? value) => GrantManagerMapperlyHelpers.IndigenousOrgIndToBool(value);
    private static string? ResolveFiscalDay(Applicant src) => src.FiscalDay?.ToString();
}

// ---------------------------------------------------------------------------
// Two-way mappers (CreateMap<...>().ReverseMap()).
// ---------------------------------------------------------------------------

[Mapper]
public partial class ZoneGroupDefinitionMapper : TwoWayMapperBase<ZoneGroupDefinition, ZoneGroupDefinitionDto>
{
    public override partial ZoneGroupDefinitionDto Map(ZoneGroupDefinition source);
    public override partial void Map(ZoneGroupDefinition source, ZoneGroupDefinitionDto destination);
    public override partial ZoneGroupDefinition ReverseMap(ZoneGroupDefinitionDto source);
    public override partial void ReverseMap(ZoneGroupDefinitionDto source, ZoneGroupDefinition destination);

    // Forward nested: ZoneTabDefinition → ZoneTabDefinitionDto (used for Tabs collection)
    private partial ZoneTabDefinitionDto MapTabDto(ZoneTabDefinition source);

    // Reverse nested: ZoneTabDefinitionDto → ZoneTabDefinition (ignore DisplayName missing from DTO)
    [MapperIgnoreTarget(nameof(ZoneTabDefinition.DisplayName))]
    private partial ZoneTabDefinition MapTab(ZoneTabDefinitionDto source);

    // Forward nested: ZoneDefinition → ZoneDefinitionDto (ignore Arguments missing from entity)
    [MapperIgnoreTarget(nameof(ZoneDefinitionDto.Arguments))]
    private partial ZoneDefinitionDto MapZoneDto(ZoneDefinition source);
}

[Mapper]
public partial class ZoneTabDefinitionMapper : TwoWayMapperBase<ZoneTabDefinition, ZoneTabDefinitionDto>
{
    public override partial ZoneTabDefinitionDto Map(ZoneTabDefinition source);
    public override partial void Map(ZoneTabDefinition source, ZoneTabDefinitionDto destination);

    [MapperIgnoreTarget(nameof(ZoneTabDefinition.DisplayName))]
    public override partial ZoneTabDefinition ReverseMap(ZoneTabDefinitionDto source);

    [MapperIgnoreTarget(nameof(ZoneTabDefinition.DisplayName))]
    public override partial void ReverseMap(ZoneTabDefinitionDto source, ZoneTabDefinition destination);

    // Forward nested: ZoneDefinition → ZoneDefinitionDto (ignore Arguments missing from entity)
    [MapperIgnoreTarget(nameof(ZoneDefinitionDto.Arguments))]
    private partial ZoneDefinitionDto MapZoneDto(ZoneDefinition source);
}

[Mapper]
public partial class ZoneDefinitionMapper : TwoWayMapperBase<ZoneDefinition, ZoneDefinitionDto>
{
    [MapperIgnoreTarget(nameof(ZoneDefinitionDto.Arguments))]
    public override partial ZoneDefinitionDto Map(ZoneDefinition source);

    [MapperIgnoreTarget(nameof(ZoneDefinitionDto.Arguments))]
    public override partial void Map(ZoneDefinition source, ZoneDefinitionDto destination);

    public override partial ZoneDefinition ReverseMap(ZoneDefinitionDto source);
    public override partial void ReverseMap(ZoneDefinitionDto source, ZoneDefinition destination);
}

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
