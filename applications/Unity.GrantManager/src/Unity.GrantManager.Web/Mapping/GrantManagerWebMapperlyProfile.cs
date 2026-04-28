using Riok.Mapperly.Abstractions;
using Unity.GrantManager.ApplicantProfile;
using Unity.GrantManager.ApplicantProfile.ProfileData;
using Unity.GrantManager.ApplicationForms;
using Unity.GrantManager.Applications;
using Unity.GrantManager.Forms;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Intakes;
using Unity.GrantManager.SettingManagement;
using Unity.GrantManager.Web.Components.ApplicationUiSettingGroup;
using Unity.GrantManager.Web.Pages.ApplicantContact;
using Unity.GrantManager.Web.Pages.ApplicationContact;
using Unity.GrantManager.Web.Pages.ApplicationForms.ViewModels;
using Unity.GrantManager.Web.Pages.Sites.ViewModels;
using Unity.GrantManager.Web.Views.Shared.Components.ApplicantInfo;
using Unity.GrantManager.Web.Views.Shared.Components.SummaryWidget;
using Unity.Payments.Suppliers;
using Volo.Abp.Mapperly;

namespace Unity.GrantManager.Web.Mapping;

[Mapper] public partial class ApplicationFormDtoToCreateUpdateApplicationFormDtoMapper : MapperBase<ApplicationFormDto, CreateUpdateApplicationFormDto> { public override partial CreateUpdateApplicationFormDto Map(ApplicationFormDto source); public override partial void Map(ApplicationFormDto source, CreateUpdateApplicationFormDto destination); }
[Mapper] public partial class CreateUpdateApplicationFormViewModelToDtoMapper : MapperBase<CreateUpdateApplicationFormViewModel, CreateUpdateApplicationFormDto> { public override partial CreateUpdateApplicationFormDto Map(CreateUpdateApplicationFormViewModel source); public override partial void Map(CreateUpdateApplicationFormViewModel source, CreateUpdateApplicationFormDto destination); }
[Mapper] public partial class ApplicationFormDtoToViewModelMapper : MapperBase<ApplicationFormDto, CreateUpdateApplicationFormViewModel> { public override partial CreateUpdateApplicationFormViewModel Map(ApplicationFormDto source); public override partial void Map(ApplicationFormDto source, CreateUpdateApplicationFormViewModel destination); }

[Mapper] public partial class ApplicationToGrantApplicationDtoWebMapper : MapperBase<Application, GrantApplicationDto> { public override partial GrantApplicationDto Map(Application source); public override partial void Map(Application source, GrantApplicationDto destination); }

[Mapper]
public partial class GetSummaryDtoToSummaryWidgetViewModelMapper : MapperBase<GetSummaryDto, SummaryWidgetViewModel>
{
    [MapPropertyFromSource(nameof(SummaryWidgetViewModel.SubmissionDate), Use = nameof(ResolveSubmissionDate))]
    public override partial SummaryWidgetViewModel Map(GetSummaryDto source);

    [MapPropertyFromSource(nameof(SummaryWidgetViewModel.SubmissionDate), Use = nameof(ResolveSubmissionDate))]
    public override partial void Map(GetSummaryDto source, SummaryWidgetViewModel destination);

    private static string ResolveSubmissionDate(GetSummaryDto src)
        => src.SubmissionDate == null ? string.Empty : src.SubmissionDate.Value.ToShortDateString();
}

[Mapper] public partial class ContactModalViewModelToDtoMapper : MapperBase<ContactModalViewModel, ApplicationContactDto> { public override partial ApplicationContactDto Map(ContactModalViewModel source); public override partial void Map(ContactModalViewModel source, ApplicationContactDto destination); }
[Mapper] public partial class ApplicationContactDtoToViewModelMapper : MapperBase<ApplicationContactDto, ContactModalViewModel> { public override partial ContactModalViewModel Map(ApplicationContactDto source); public override partial void Map(ApplicationContactDto source, ContactModalViewModel destination); }

[Mapper] public partial class ApplicantSummaryDtoToViewModelMapper : MapperBase<ApplicantSummaryDto, ApplicantSummaryViewModel> { public override partial ApplicantSummaryViewModel Map(ApplicantSummaryDto source); public override partial void Map(ApplicantSummaryDto source, ApplicantSummaryViewModel destination); }
[Mapper] public partial class ContactInfoDtoToViewModelMapper : MapperBase<ContactInfoDto, ContactInfoViewModel> { public override partial ContactInfoViewModel Map(ContactInfoDto source); public override partial void Map(ContactInfoDto source, ContactInfoViewModel destination); }
[Mapper] public partial class SigningAuthorityDtoToViewModelMapper : MapperBase<SigningAuthorityDto, SigningAuthorityViewModel> { public override partial SigningAuthorityViewModel Map(SigningAuthorityDto source); public override partial void Map(SigningAuthorityDto source, SigningAuthorityViewModel destination); }
[Mapper] public partial class SiteDtoToCreateUpdateSiteViewModelMapper : MapperBase<SiteDto, CreateUpdateSiteViewModel> { public override partial CreateUpdateSiteViewModel Map(SiteDto source); public override partial void Map(SiteDto source, CreateUpdateSiteViewModel destination); }
[Mapper] public partial class CreateUpdateSiteViewModelToDtoMapper : MapperBase<CreateUpdateSiteViewModel, SiteDto> { public override partial SiteDto Map(CreateUpdateSiteViewModel source); public override partial void Map(CreateUpdateSiteViewModel source, SiteDto destination); }

[Mapper]
public partial class ApplicantAddressDtoToViewModelMapper : MapperBase<ApplicantAddressDto, ApplicantAddressViewModel>
{
    [MapProperty(nameof(ApplicantAddressDto.Postal), nameof(ApplicantAddressViewModel.PostalCode))]
    public override partial ApplicantAddressViewModel Map(ApplicantAddressDto source);

    [MapProperty(nameof(ApplicantAddressDto.Postal), nameof(ApplicantAddressViewModel.PostalCode))]
    public override partial void Map(ApplicantAddressDto source, ApplicantAddressViewModel destination);
}

[Mapper] public partial class IntakeDtoToCreateUpdateIntakeDtoMapper : MapperBase<IntakeDto, CreateUpdateIntakeDto> { public override partial CreateUpdateIntakeDto Map(IntakeDto source); public override partial void Map(IntakeDto source, CreateUpdateIntakeDto destination); }

[Mapper] public partial class ApplicationUiSettingsDtoToViewModelMapper : MapperBase<ApplicationUiSettingsDto, ApplicationUiSettingsViewModel> { public override partial ApplicationUiSettingsViewModel Map(ApplicationUiSettingsDto source); public override partial void Map(ApplicationUiSettingsDto source, ApplicationUiSettingsViewModel destination); }

[Mapper]
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
