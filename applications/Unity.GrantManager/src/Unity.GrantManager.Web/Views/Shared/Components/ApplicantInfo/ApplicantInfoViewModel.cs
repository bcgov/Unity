using AutoMapper.Configuration.Annotations;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Unity.GrantManager.GrantApplications;
using Unity.GrantManager.Locality;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantInfo;

public class ApplicantInfoViewModel : PageModel
{
    public List<SectorDto> ApplicationSectors { get; set; } = new List<SectorDto>();
    public List<SelectListItem> ApplicationSectorsList { get; set; } = new List<SelectListItem>();
    public List<SelectListItem> ApplicationSubSectorsList { get; set; } = new List<SelectListItem>();

    public List<SelectListItem> OrganizationTypeListOptions { get; set; } = ProjectInfoOptionsList.OrganizationTypeList.FormatOptionsList<SelectListItem>();
    public List<SelectListItem> OrgBookStatusListOptions { get; set; } = ProjectInfoOptionsList.OrgBookStatusList.FormatOptionsList<SelectListItem>();
    public List<SelectListItem> IndigenousListOptions { get; set; } = ApplicantInfoOptionsList.IndigenousList.FormatOptionsList<SelectListItem>();
    public List<SelectListItem> FiscalDayListOptions { get; set; } = ApplicantInfoOptionsList.FiscalDayList.FormatOptionsList<SelectListItem>();
    //.FormatOptionsList<SelectListItem>(orderBy: x => int.Parse(x.Value));
    public List<SelectListItem> FiscalMonthListOptions { get; set; } = ApplicantInfoOptionsList.FiscalMonthList.FormatOptionsList<SelectListItem>();
    //.FormatOptionsList<SelectListItem>(x => DateTime.ParseExact(x.Text, "MMMM", CultureInfo.InvariantCulture).Month);

    public Guid ApplicationId { get; set; }
    public Guid ApplicantId { get; set; }
    public Guid ApplicationFormId { get; set; }
    public Guid ApplicationFormVersionId { get; set; }

    public bool IsFinalDecisionMade { get; set; }
    [Display(Name = "ApplicantInfoView:ApplicantInfo.Search")]
    public string? Search { get; set; }
    public string? SelectedOrgBookId { get; set; }
    public string? SelectedOrgBookText { get; set; }

    // New Model
    public ApplicantSummaryViewModel ApplicantSummary { get; set; } = new ApplicantSummaryViewModel();
    public List<ApplicantAddressViewModel> ApplicantAddresses { get; set; } = new List<ApplicantAddressViewModel>();
    public SigningAuthorityViewModel SigningAuthority { get; set; } = new SigningAuthorityViewModel();
    public ContactInfoViewModel ContactInfo { get; set; } = new ContactInfoViewModel();

    [Ignore]
    public ApplicantAddressViewModel PhysicalAddress { get; set; } = new ApplicantAddressViewModel();
    [Ignore]
    public ApplicantAddressViewModel MailingAddress { get; set; } = new ApplicantAddressViewModel();

    public class ApplicantSupplierViewModel
    {
        [Display(Name = "ApplicantInfoView:ApplicantInfo.SupplierNumber")]
        public string? SupplierNumber { get; set; }

        [Display(Name = "ApplicantInfoView:ApplicantInfo.OriginalSupplierNumber")]
        public string? OriginalSupplierNumber { get; set; }
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
        [SelectItems(nameof(OrgBookStatusListOptions))]
        public string? OrgStatus { get; set; }

        [Display(Name = "ApplicantInfoView:ApplicantInfo.OrganizationType")]
        [SelectItems(nameof(OrganizationType))]
        public string? OrganizationType { get; set; }


        [Display(Name = "ApplicantInfoView:ApplicantInfo.NonRegOrgName")]
        public string? NonRegOrgName { get; set; }

        [Display(Name = "ApplicantInfoView:ApplicantInfo.OrganizationSize")]
        public string? OrganizationSize { get; set; }

        [Display(Name = "ApplicantInfoView:ApplicantInfo.IndigenousOrgInd")]
        [SelectItems(nameof(ApplicantInfoViewModel.IndigenousListOptions))]
        public string? IndigenousOrgInd { get; set; }

        [Display(Name = "ApplicantInfoView:ApplicantInfo.UnityApplicant")]
        public string? UnityApplicantId { get; set; }

        [Display(Name = "ApplicantInfoView:ApplicantInfo.FiscalMonth")]
        [SelectItems(nameof(FiscalMonthListOptions))]
        public string? FiscalMonth { get; set; }
        [Display(Name = "ApplicantInfoView:ApplicantInfo.FiscalDay")]
        [SelectItems(nameof(FiscalDayListOptions))]
        public string? FiscalDay { get; set; }


        [Display(Name = "ApplicantInfoView:ApplicantInfo.Sector")]
        [SelectItems(nameof(ApplicationSectorsList))] // TODO
        public string? Sector { get; set; }

        [Display(Name = "ApplicantInfoView:ApplicantInfo.SubSector")]
        [SelectItems(nameof(ApplicationSubSectorsList))] // TODO
        public string? SubSector { get; set; }

        [Display(Name = "ApplicantInfoView:ApplicantInfo.SectorSubSectorIndustryDesc")]
        [TextArea(Rows = 2)]
        public string? SectorSubSectorIndustryDesc { get; set; }

        public bool RedStop { get; set; }
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
        //[RegularExpression(@"^(\+\s?)?((?<!\+.*)\(\+?\d+([\s\-\.]?\d+)?\)|\d+)([\s\-\.]?(\(\d+([\s\-\.]?\d+)?\)|\d+))*(\s?(x|ext\.?)\s?\d+)?$", ErrorMessage = "Invalid Phone Number.")]
        //[Phone(ErrorMessage = "Invalid Phone Number.")]
        public string? SigningAuthorityBusinessPhone { get; set; }

        [Display(Name = "ApplicantInfoView:ApplicantInfo.SigningAuthorityCellPhone")]
        [DataType(DataType.PhoneNumber, ErrorMessage = "Invalid Phone Number")]
        //[RegularExpression(@"^(\+\s?)?((?<!\+.*)\(\+?\d+([\s\-\.]?\d+)?\)|\d+)([\s\-\.]?(\(\d+([\s\-\.]?\d+)?\)|\d+))*(\s?(x|ext\.?)\s?\d+)?$", ErrorMessage = "Invalid Phone Number.")]
        //[Phone(ErrorMessage = "Invalid Phone Number.")]
        public string? SigningAuthorityCellPhone { get; set; }
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
        //[RegularExpression(@"^(\+\s?)?((?<!\+.*)\(\+?\d+([\s\-\.]?\d+)?\)|\d+)([\s\-\.]?(\(\d+([\s\-\.]?\d+)?\)|\d+))*(\s?(x|ext\.?)\s?\d+)?$", ErrorMessage = "Invalid Phone Number.")]
        public string? Phone { get; set; }

        [Display(Name = "ApplicantInfoView:ApplicantInfo.ContactCellPhone")]
        [DataType(DataType.PhoneNumber, ErrorMessage = "Invalid Phone Number")]
        //[RegularExpression(@"^(\+\s?)?((?<!\+.*)\(\+?\d+([\s\-\.]?\d+)?\)|\d+)([\s\-\.]?(\(\d+([\s\-\.]?\d+)?\)|\d+))*(\s?(x|ext\.?)\s?\d+)?$", ErrorMessage = "Invalid Phone Number.")]
        public string? Phone2 { get; set; }
    }

}