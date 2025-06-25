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
    public SigningAuthorityViewModel SigningAuthority { get; set; } = new SigningAuthorityViewModel();
    public ContactInfoViewModel ContactInfo { get; set; } = new ContactInfoViewModel();
    public ApplicantAddressViewModel PhysicalAddress { get; set; } = new ApplicantAddressViewModel();
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
