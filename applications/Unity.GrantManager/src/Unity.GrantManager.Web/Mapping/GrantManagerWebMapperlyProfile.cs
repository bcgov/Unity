using Riok.Mapperly.Abstractions;
using System;
using Unity.GrantManager.ApplicantProfile;
using Unity.GrantManager.ApplicantProfile.ProfileData;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Forms;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Identity;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.SettingManagement;
using Unity.GrantManager.Web.Components.ApplicationUiSettingGroup;
using Unity.GrantManager.Web.Pages.ApplicantContact;
using Unity.GrantManager.Web.Pages.ApplicationContact;
using Unity.GrantManager.Web.Pages.ApplicationForms.ViewModels;
using Unity.GrantManager.Web.Pages.BulkActions;
using Unity.GrantManager.Web.Pages.Sites.ViewModels;
using Unity.GrantManager.Web.Views.Shared.Components.ApplicantInfo;
using Unity.GrantManager.Web.Views.Shared.Components.SummaryWidget;
using Unity.Payments.Suppliers;
using Volo.Abp.Mapperly;

namespace Unity.GrantManager.Web.Mapping;

[Mapper] 
public partial class ApplicationFormDtoToCreateUpdateApplicationFormDtoMapper : MapperBase<ApplicationFormDto, CreateUpdateApplicationFormDto> 
{ 
    public override partial CreateUpdateApplicationFormDto Map(ApplicationFormDto source); 
    public override partial void Map(ApplicationFormDto source, CreateUpdateApplicationFormDto destination); 
}

[Mapper] 
public partial class CreateUpdateApplicationFormViewModelToDtoMapper : MapperBase<CreateUpdateApplicationFormViewModel, CreateUpdateApplicationFormDto> 
{
    [MapperIgnoreTarget(nameof(CreateUpdateApplicationFormDto.ChefsCriteriaFormGuid))]
    [MapperIgnoreTarget(nameof(CreateUpdateApplicationFormDto.Version))]
    [MapperIgnoreTarget(nameof(CreateUpdateApplicationFormDto.Payable))]
    public override partial CreateUpdateApplicationFormDto Map(CreateUpdateApplicationFormViewModel source);

    [MapperIgnoreTarget(nameof(CreateUpdateApplicationFormDto.ChefsCriteriaFormGuid))]
    [MapperIgnoreTarget(nameof(CreateUpdateApplicationFormDto.Version))]
    [MapperIgnoreTarget(nameof(CreateUpdateApplicationFormDto.Payable))]
    public override partial void Map(CreateUpdateApplicationFormViewModel source, CreateUpdateApplicationFormDto destination); 
}

[Mapper] 
public partial class ApplicationFormDtoToViewModelMapper : MapperBase<ApplicationFormDto, CreateUpdateApplicationFormViewModel> 
{
    [MapperIgnoreTarget(nameof(CreateUpdateApplicationFormViewModel.IntakesList))]
    public override partial CreateUpdateApplicationFormViewModel Map(ApplicationFormDto source);

    [MapperIgnoreTarget(nameof(CreateUpdateApplicationFormViewModel.IntakesList))]
    public override partial void Map(ApplicationFormDto source, CreateUpdateApplicationFormViewModel destination); 
}

[Mapper(AllowNullPropertyAssignment = true)]
public partial class ApplicationToGrantApplicationDtoWebMapper : MapperBase<Application, GrantApplicationDto> 
{
    [MapperIgnoreTarget(nameof(GrantApplicationDto.RowCount))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.Assignees))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.Status))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.Probability))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.ApplicationName))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.Category))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.Sector))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.SubSector))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.SubStatusDisplayValue))]
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
    [MapperIgnoreTarget(nameof(GrantApplicationDto.ApproxNumberOfEmployees))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.OrgNumber))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.SectorSubSectorIndustryDesc))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.PaymentInfo))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.AIAnalysisData))]    
    public override partial GrantApplicationDto Map(Application source);


    [MapperIgnoreTarget(nameof(GrantApplicationDto.RowCount))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.Assignees))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.Status))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.Probability))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.ApplicationName))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.Category))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.Sector))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.SubSector))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.SubStatusDisplayValue))]
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
    [MapperIgnoreTarget(nameof(GrantApplicationDto.ApproxNumberOfEmployees))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.OrgNumber))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.SectorSubSectorIndustryDesc))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.PaymentInfo))]
    [MapperIgnoreTarget(nameof(GrantApplicationDto.AIAnalysisData))]    
    public override partial void Map(Application source, GrantApplicationDto destination);

    [MapperIgnoreTarget(nameof(GrantApplicationApplicantDto.SiteId))]
    [MapperIgnoreTarget(nameof(GrantApplicationApplicantDto.ElectoralDistrict))]
    [MapPropertyFromSource(nameof(GrantApplicationApplicantDto.FiscalDay), Use = nameof(ResolveApplicantFiscalDay))]
    [MapPropertyFromSource(nameof(GrantApplicationApplicantDto.SupplierId), Use = nameof(ResolveApplicantSupplierId))]
    [MapPropertyFromSource(nameof(GrantApplicationApplicantDto.RedStop), Use = nameof(ResolveApplicantRedStop))]
    private partial GrantApplicationApplicantDto ToDto(Applicant source);

    [MapperIgnoreTarget(nameof(ApplicationFormDto.ChefsFormVersionGuid))]
    [MapperIgnoreTarget(nameof(ApplicationFormDto.SubmissionHeaderMapping))]
    [MapperIgnoreTarget(nameof(ApplicationFormDto.ApiToken))]
    private partial ApplicationFormDto ToDto(ApplicationForm source);

    [MapperIgnoreTarget(nameof(GrantApplicationAssigneeDto.AssigneeId))]
    [MapperIgnoreTarget(nameof(GrantApplicationAssigneeDto.ApplicationId))]
    [MapperIgnoreTarget(nameof(GrantApplicationAssigneeDto.Duty))]
    [MapperIgnoreTarget(nameof(GrantApplicationAssigneeDto.Email))]
    private partial GrantApplicationAssigneeDto ToDto(Person source);

    private static string ResolveApplicantFiscalDay(Applicant src) => src.FiscalDay?.ToString() ?? string.Empty;
    private static Guid ResolveApplicantSupplierId(Applicant src) => src.SupplierId ?? Guid.Empty;
    private static bool ResolveApplicantRedStop(Applicant src) => src.RedStop ?? false;
}

[Mapper(AllowNullPropertyAssignment = true)]
public partial class GetSummaryDtoToSummaryWidgetViewModelMapper : MapperBase<GetSummaryDto, SummaryWidgetViewModel>
{
    [MapPropertyFromSource(nameof(SummaryWidgetViewModel.SubmissionDate), Use = nameof(ResolveSubmissionDate))]
    [MapPropertyFromSource(nameof(SummaryWidgetViewModel.TotalScore), Use = nameof(ResolveTotalScore))]
    [MapperIgnoreTarget(nameof(SummaryWidgetViewModel.ApplicationId))]
    [MapperIgnoreTarget(nameof(SummaryWidgetViewModel.IsReadOnly))]
    public override partial SummaryWidgetViewModel Map(GetSummaryDto source);

    [MapPropertyFromSource(nameof(SummaryWidgetViewModel.SubmissionDate), Use = nameof(ResolveSubmissionDate))]
    [MapPropertyFromSource(nameof(SummaryWidgetViewModel.TotalScore), Use = nameof(ResolveTotalScore))]
    [MapperIgnoreTarget(nameof(SummaryWidgetViewModel.ApplicationId))]
    [MapperIgnoreTarget(nameof(SummaryWidgetViewModel.IsReadOnly))]
    public override partial void Map(GetSummaryDto source, SummaryWidgetViewModel destination);

    private static string ResolveSubmissionDate(GetSummaryDto src)
        => src.SubmissionDate == null ? string.Empty : src.SubmissionDate.Value.ToShortDateString();

    private static int ResolveTotalScore(GetSummaryDto src)
        => int.TryParse(src.TotalScore, out var result) ? result : 0;
}

[Mapper(AllowNullPropertyAssignment = true)]
public partial class ContactModalViewModelToDtoMapper : MapperBase<ContactModalViewModel, ApplicationContactDto>
{
    public override partial ApplicationContactDto Map(ContactModalViewModel source);
    public override partial void Map(ContactModalViewModel source, ApplicationContactDto destination);
}

[Mapper]
public partial class ApplicationContactDtoToViewModelMapper : MapperBase<ApplicationContactDto, ContactModalViewModel>
{
    [MapperIgnoreTarget(nameof(ContactModalViewModel.ContactTypeList))]
    public override partial ContactModalViewModel Map(ApplicationContactDto source);

    [MapperIgnoreTarget(nameof(ContactModalViewModel.ContactTypeList))]
    public override partial void Map(ApplicationContactDto source, ContactModalViewModel destination);
}

[Mapper(AllowNullPropertyAssignment = true)]
public partial class ApplicantSummaryDtoToViewModelMapper : MapperBase<ApplicantSummaryDto, ApplicantSummaryViewModel>
{
    public override partial ApplicantSummaryViewModel Map(ApplicantSummaryDto source);
    public override partial void Map(ApplicantSummaryDto source, ApplicantSummaryViewModel destination);
}

[Mapper] 
public partial class ContactInfoDtoToViewModelMapper : MapperBase<ContactInfoDto, ContactInfoViewModel> 
{ 
    public override partial ContactInfoViewModel Map(ContactInfoDto source); 
    public override partial void Map(ContactInfoDto source, ContactInfoViewModel destination); 
}

[Mapper] 
public partial class SigningAuthorityDtoToViewModelMapper : MapperBase<SigningAuthorityDto, SigningAuthorityViewModel> 
{ 
    public override partial SigningAuthorityViewModel Map(SigningAuthorityDto source); 
    public override partial void Map(SigningAuthorityDto source, SigningAuthorityViewModel destination); 
}

[Mapper]
public partial class SiteDtoToCreateUpdateSiteViewModelMapper : MapperBase<SiteDto, CreateUpdateSiteViewModel>
{
    [MapperIgnoreTarget(nameof(CreateUpdateSiteViewModel.MailingAddress))]
    public override partial CreateUpdateSiteViewModel Map(SiteDto source);

    [MapperIgnoreTarget(nameof(CreateUpdateSiteViewModel.MailingAddress))]
    public override partial void Map(SiteDto source, CreateUpdateSiteViewModel destination);
}

[Mapper]
public partial class CreateUpdateSiteViewModelToDtoMapper : MapperBase<CreateUpdateSiteViewModel, SiteDto>
{
    [MapperIgnoreTarget(nameof(SiteDto.AddressLine1))]
    [MapperIgnoreTarget(nameof(SiteDto.AddressLine2))]
    [MapperIgnoreTarget(nameof(SiteDto.AddressLine3))]
    [MapperIgnoreTarget(nameof(SiteDto.City))]
    [MapperIgnoreTarget(nameof(SiteDto.Province))]
    [MapperIgnoreTarget(nameof(SiteDto.PostalCode))]
    [MapperIgnoreTarget(nameof(SiteDto.SupplierId))]
    [MapperIgnoreTarget(nameof(SiteDto.Country))]
    [MapperIgnoreTarget(nameof(SiteDto.EmailAddress))]
    [MapperIgnoreTarget(nameof(SiteDto.EFTAdvicePref))]
    [MapperIgnoreTarget(nameof(SiteDto.ProviderId))]
    [MapperIgnoreTarget(nameof(SiteDto.SiteProtected))]
    [MapperIgnoreTarget(nameof(SiteDto.LastUpdatedInCas))]
    [MapperIgnoreTarget(nameof(SiteDto.MarkDeletedInUse))]
    [MapperIgnoreTarget(nameof(SiteDto.CreationTime))]
    [MapperIgnoreTarget(nameof(SiteDto.CreatorId))]
    [MapperIgnoreTarget(nameof(SiteDto.LastModificationTime))]
    [MapperIgnoreTarget(nameof(SiteDto.LastModifierId))]
    public override partial SiteDto Map(CreateUpdateSiteViewModel source);

    [MapperIgnoreTarget(nameof(SiteDto.AddressLine1))]
    [MapperIgnoreTarget(nameof(SiteDto.AddressLine2))]
    [MapperIgnoreTarget(nameof(SiteDto.AddressLine3))]
    [MapperIgnoreTarget(nameof(SiteDto.City))]
    [MapperIgnoreTarget(nameof(SiteDto.Province))]
    [MapperIgnoreTarget(nameof(SiteDto.PostalCode))]
    [MapperIgnoreTarget(nameof(SiteDto.SupplierId))]
    [MapperIgnoreTarget(nameof(SiteDto.Country))]
    [MapperIgnoreTarget(nameof(SiteDto.EmailAddress))]
    [MapperIgnoreTarget(nameof(SiteDto.EFTAdvicePref))]
    [MapperIgnoreTarget(nameof(SiteDto.ProviderId))]
    [MapperIgnoreTarget(nameof(SiteDto.SiteProtected))]
    [MapperIgnoreTarget(nameof(SiteDto.LastUpdatedInCas))]
    [MapperIgnoreTarget(nameof(SiteDto.MarkDeletedInUse))]
    [MapperIgnoreTarget(nameof(SiteDto.CreationTime))]
    [MapperIgnoreTarget(nameof(SiteDto.CreatorId))]
    [MapperIgnoreTarget(nameof(SiteDto.LastModificationTime))]
    [MapperIgnoreTarget(nameof(SiteDto.LastModifierId))]
    public override partial void Map(CreateUpdateSiteViewModel source, SiteDto destination);
}

[Mapper]
public partial class ApplicantAddressDtoToViewModelMapper : MapperBase<ApplicantAddressDto, ApplicantAddressViewModel>
{
    [MapProperty(nameof(ApplicantAddressDto.Postal), nameof(ApplicantAddressViewModel.PostalCode))]
    [MapperIgnoreTarget(nameof(ApplicantAddressViewModel.ApplicantAddressId))]
    public override partial ApplicantAddressViewModel Map(ApplicantAddressDto source);

    [MapProperty(nameof(ApplicantAddressDto.Postal), nameof(ApplicantAddressViewModel.PostalCode))]
    [MapperIgnoreTarget(nameof(ApplicantAddressViewModel.ApplicantAddressId))]
    public override partial void Map(ApplicantAddressDto source, ApplicantAddressViewModel destination);
}

[Mapper] 
public partial class IntakeDtoToCreateUpdateIntakeDtoMapper : MapperBase<IntakeDto, CreateUpdateIntakeDto> 
{ 
    public override partial CreateUpdateIntakeDto Map(IntakeDto source); 
    public override partial void Map(IntakeDto source, CreateUpdateIntakeDto destination); 
}

[Mapper] 
public partial class ApplicationUiSettingsDtoToViewModelMapper : MapperBase<ApplicationUiSettingsDto, ApplicationUiSettingsViewModel> 
{ 
    public override partial ApplicationUiSettingsViewModel Map(ApplicationUiSettingsDto source); 
    public override partial void Map(ApplicationUiSettingsDto source, ApplicationUiSettingsViewModel destination); 
}

[Mapper(AllowNullPropertyAssignment = true)]
public partial class ContactInfoItemDtoToApplicantContactModalViewModelMapper : MapperBase<ContactInfoItemDto, ApplicantContactModalViewModel>
{
    [MapProperty(nameof(ContactInfoItemDto.ContactId), nameof(ApplicantContactModalViewModel.Id))]
    [MapperIgnoreSource(nameof(ContactInfoItemDto.IsEditable))]
    [MapperIgnoreSource(nameof(ContactInfoItemDto.ApplicationId))]
    [MapperIgnoreSource(nameof(ContactInfoItemDto.ReferenceNo))]
    [MapperIgnoreSource(nameof(ContactInfoItemDto.CreationTime))]
    [MapperIgnoreSource(nameof(ContactInfoItemDto.HomePhoneNumber))]
    [MapperIgnoreSource(nameof(ContactInfoItemDto.WorkPhoneExtension))]
    [MapperIgnoreSource(nameof(ContactInfoItemDto.ContactType))]
    [MapperIgnoreTarget(nameof(ApplicantContactModalViewModel.ApplicantId))]
    [MapperIgnoreTarget(nameof(ApplicantContactModalViewModel.RoleOptions))]
    public override partial ApplicantContactModalViewModel Map(ContactInfoItemDto source);

    [MapProperty(nameof(ContactInfoItemDto.ContactId), nameof(ApplicantContactModalViewModel.Id))]
    [MapperIgnoreSource(nameof(ContactInfoItemDto.IsEditable))]
    [MapperIgnoreSource(nameof(ContactInfoItemDto.ApplicationId))]
    [MapperIgnoreSource(nameof(ContactInfoItemDto.ReferenceNo))]
    [MapperIgnoreSource(nameof(ContactInfoItemDto.CreationTime))]
    [MapperIgnoreSource(nameof(ContactInfoItemDto.HomePhoneNumber))]
    [MapperIgnoreSource(nameof(ContactInfoItemDto.WorkPhoneExtension))]
    [MapperIgnoreSource(nameof(ContactInfoItemDto.ContactType))]
    [MapperIgnoreTarget(nameof(ApplicantContactModalViewModel.ApplicantId))]
    [MapperIgnoreTarget(nameof(ApplicantContactModalViewModel.RoleOptions))]
    public override partial void Map(ContactInfoItemDto source, ApplicantContactModalViewModel destination);
}

[Mapper]
public partial class ApplicantContactModalViewModelToUpdateApplicantContactDtoMapper : MapperBase<ApplicantContactModalViewModel, UpdateApplicantContactDto>
{
    [MapperIgnoreSource(nameof(ApplicantContactModalViewModel.ApplicantId))]
    [MapperIgnoreSource(nameof(ApplicantContactModalViewModel.Id))]
    [MapperIgnoreSource(nameof(ApplicantContactModalViewModel.IsPrimaryInferred))]
    [MapperIgnoreSource(nameof(ApplicantContactModalViewModel.RoleOptions))]
    [MapperIgnoreTarget(nameof(UpdateApplicantContactDto.WorkPhoneExtension))]
    public override partial UpdateApplicantContactDto Map(ApplicantContactModalViewModel source);

    [MapperIgnoreSource(nameof(ApplicantContactModalViewModel.ApplicantId))]
    [MapperIgnoreSource(nameof(ApplicantContactModalViewModel.Id))]
    [MapperIgnoreSource(nameof(ApplicantContactModalViewModel.IsPrimaryInferred))]
    [MapperIgnoreSource(nameof(ApplicantContactModalViewModel.RoleOptions))]
    [MapperIgnoreTarget(nameof(UpdateApplicantContactDto.WorkPhoneExtension))]
    public override partial void Map(ApplicantContactModalViewModel source, UpdateApplicantContactDto destination);
}

[Mapper]
public partial class BulkPublishDtoToViewModelMapper : MapperBase<BulkPublishDto, BulkPublishApplicationViewModel>
{
    [MapperIgnoreTarget(nameof(BulkPublishApplicationViewModel.IsValid))]
    public override partial BulkPublishApplicationViewModel Map(BulkPublishDto source);

    [MapperIgnoreTarget(nameof(BulkPublishApplicationViewModel.IsValid))]
    public override partial void Map(BulkPublishDto source, BulkPublishApplicationViewModel destination);
}
