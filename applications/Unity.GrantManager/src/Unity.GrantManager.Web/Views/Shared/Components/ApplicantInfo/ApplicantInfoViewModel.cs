using AutoMapper.Configuration.Annotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Locality;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantInfo;

public class ApplicantInfoViewModel
{
    public List<SelectListItem> OrganizationTypeList { get; set; } = FormatOptionsList(ProjectInfoOptionsList.OrganizationTypeList);
    public List<SelectListItem> OrgBookStatusList { get; set; } = FormatOptionsList(ProjectInfoOptionsList.OrgBookStatusList);
    public List<SelectListItem> ApplicationSectorsList { get; set; } = [];
    public List<SelectListItem> ApplicationSubSectorsList { get; set; } = [];
    public List<SelectListItem> IndigenousList { get; set; } = FormatOptionsList(ApplicantInfoOptionsList.IndigenousList);
    public List<SelectListItem> FiscalDayList { get; set; } = [.. FormatOptionsList(ApplicantInfoOptionsList.FiscalDayList).OrderBy(x => int.Parse(x.Text))];
    public List<SelectListItem> FiscalMonthList { get; set; } = [.. FormatOptionsList(ApplicantInfoOptionsList.FiscalMonthList).OrderBy(x => DateTime.ParseExact(x.Text, "MMMM", CultureInfo.InvariantCulture).Month)];
    public List<SelectListItem> ElectoralDistrictList { get; set; } = [];

    public Guid ApplicationId { get; set; }
    public Guid ApplicantId { get; set; }
    public Guid ApplicationFormId { get; set; }
    public Guid ApplicationFormVersionId { get; set; }
    [Display(Name = "ApplicantInfoView:ApplicantElectoralDistrict")]
    [SelectItems(nameof(ElectoralDistrictList))]
    public string? ElectoralDistrict { get; set; }

    public List<SectorDto> ApplicationSectors { get; set; } = [];

    // Core Model
    public ApplicantSummaryViewModel ApplicantSummary { get; set; } = new ApplicantSummaryViewModel();
    public List<ApplicantAddressViewModel> ApplicantAddresses { get; set; } = new List<ApplicantAddressViewModel>();
    public SigningAuthorityViewModel SigningAuthority { get; set; } = new SigningAuthorityViewModel();
    public ContactInfoViewModel ContactInfo { get; set; } = new ContactInfoViewModel();

    [Ignore]
    public ApplicantAddressViewModel PhysicalAddress { get; set; } = new ApplicantAddressViewModel();
    [Ignore]
    public ApplicantAddressViewModel MailingAddress { get; set; } = new ApplicantAddressViewModel();

    public AddressType ApplicantElectoralAddressType { get; set; } = AddressType.PhysicalAddress;
    public string ApplicantElectoralAddressTypeDisplay
    {
        get
        {
            return ApplicantElectoralAddressType switch
            {
                AddressType.PhysicalAddress => "Physical Address",
                AddressType.MailingAddress => "Mailing Address",
                AddressType.BusinessAddress => "Business Address",
                _ => "Address"
            };
        }
    }

    

    [Display(Name = "ApplicantInfoView:ApplicantInfo.Search")]
    public string? Search { get; set; }

    public string? SelectedOrgBookId { get; set; }

    public string? SelectedOrgBookText { get; set; }

    public string? SelectedApplicantLookUp { get; set; }

    

    public static List<SelectListItem> FormatOptionsList(ImmutableDictionary<string, string> optionsList)
    {
        List<SelectListItem> optionsFormattedList = [];
        foreach (KeyValuePair<string, string> entry in optionsList)
        {
            optionsFormattedList.Add(new SelectListItem { Value = entry.Key, Text = entry.Value });
        }
        return optionsFormattedList;
    }
}

public class ApplicantSummaryViewModel
{
    // ApplicantSummary is a sub-set of Applicant
    public Guid ApplicantId { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.OrgName")]
    public string? OrgName { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.OrgNumber")]
    public string? OrgNumber { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.OrgBookStatus")]
    [SelectItems(nameof(ApplicantInfoViewModel.OrgBookStatusList))]
    public string? OrgStatus { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.OrganizationType")]
    [SelectItems(nameof(ApplicantInfoViewModel.OrganizationTypeList))]
    public string? OrganizationType { get; set; }


    [Display(Name = "ApplicantInfoView:ApplicantInfo.NonRegOrgName")]
    public string? NonRegOrgName { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.OrganizationSize")]
    public string? OrganizationSize { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.IndigenousOrgInd")]
    [SelectItems(nameof(ApplicantInfoViewModel.IndigenousList))]
    public string? IndigenousOrgInd { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.UnityApplicant")]
    public string? UnityApplicantId { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.FiscalMonth")]
    [SelectItems(nameof(ApplicantInfoViewModel.FiscalMonthList))]
    public string? FiscalMonth { get; set; }
    [Display(Name = "ApplicantInfoView:ApplicantInfo.FiscalDay")]
    [SelectItems(nameof(ApplicantInfoViewModel.FiscalDayList))]
    public string? FiscalDay { get; set; }


    [Display(Name = "ApplicantInfoView:ApplicantInfo.Sector")]
    [SelectItems(nameof(ApplicantInfoViewModel.ApplicationSectorsList))]
    public string? Sector { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.SubSector")]
    [SelectItems(nameof(ApplicantInfoViewModel.ApplicationSubSectorsList))]
    public string? SubSector { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.SectorSubSectorIndustryDesc")]
    [TextArea(Rows = 2)]
    public string? SectorSubSectorIndustryDesc { get; set; }

    public bool RedStop { get; set; }
}

public class SigningAuthorityViewModel
{
    [Display(Name = "ApplicantInfoView:ApplicantInfo.SigningAuthorityFullName")]
    [MaxLength(600, ErrorMessage = "Must be a maximum of 6 characters")]
    public string? SigningAuthorityFullName { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.SigningAuthorityTitle")]
    public string? SigningAuthorityTitle { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.SigningAuthorityEmail")]
    [DataType(DataType.EmailAddress, ErrorMessage = "Provided email is not valid")]
    public string? SigningAuthorityEmail { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.SigningAuthorityBusinessPhone")]
    [DataType(DataType.PhoneNumber, ErrorMessage = "Invalid Phone Number")]
    [RegularExpression(@"^(\+\s?)?((?<!\+.*)\(\+?\d+([\s\-\.]?\d+)?\)|\d+)([\s\-\.]?(\(\d+([\s\-\.]?\d+)?\)|\d+))*(\s?(x|ext\.?)\s?\d+)?$", ErrorMessage = "Invalid Phone Number.")]
    public string? SigningAuthorityBusinessPhone { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.SigningAuthorityCellPhone")]
    [DataType(DataType.PhoneNumber, ErrorMessage = "Invalid Phone Number")]
    [RegularExpression(@"^(\+\s?)?((?<!\+.*)\(\+?\d+([\s\-\.]?\d+)?\)|\d+)([\s\-\.]?(\(\d+([\s\-\.]?\d+)?\)|\d+))*(\s?(x|ext\.?)\s?\d+)?$", ErrorMessage = "Invalid Phone Number.")]
    public string? SigningAuthorityCellPhone { get; set; }
}

public class ApplicantAddressViewModel
{
    public Guid ApplicantAddressId { get; set; }
    public Guid ApplicantId { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.AddressType")]
    public AddressType AddressType { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.Street")]
    public string Street { get; set; } = string.Empty;

    [Display(Name = "ApplicantInfoView:ApplicantInfo.Street2")]
    public string Street2 { get; set; } = string.Empty;

    [Display(Name = "ApplicantInfoView:ApplicantInfo.Unit")]
    public string? Unit { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.City")]
    public string? City { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.Province")]
    public string? Province { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.PostalCode")]
    public string? PostalCode { get; set; }
}

public class ContactInfoViewModel
{
    public Guid? ApplicantAgentId { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.ContactFullName")]
    [MaxLength(600, ErrorMessage = "Must be a maximum of 6 characters")]
    public string? Name { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.ContactTitle")]
    public string? Title { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.ContactEmail")]
    [DataType(DataType.EmailAddress, ErrorMessage = "Provided email is not valid")]
    public string? Email { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.ContactBusinessPhone")]
    [DataType(DataType.PhoneNumber, ErrorMessage = "Invalid Phone Number")]
    [RegularExpression(@"^(\+\s?)?((?<!\+.*)\(\+?\d+([\s\-\.]?\d+)?\)|\d+)([\s\-\.]?(\(\d+([\s\-\.]?\d+)?\)|\d+))*(\s?(x|ext\.?)\s?\d+)?$", ErrorMessage = "Invalid Phone Number.")]
    public string? Phone { get; set; }

    [Display(Name = "ApplicantInfoView:ApplicantInfo.ContactCellPhone")]
    [DataType(DataType.PhoneNumber, ErrorMessage = "Invalid Phone Number")]
    [RegularExpression(@"^(\+\s?)?((?<!\+.*)\(\+?\d+([\s\-\.]?\d+)?\)|\d+)([\s\-\.]?(\(\d+([\s\-\.]?\d+)?\)|\d+))*(\s?(x|ext\.?)\s?\d+)?$", ErrorMessage = "Invalid Phone Number.")]
    public string? Phone2 { get; set; }
}

