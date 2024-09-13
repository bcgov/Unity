using System.ComponentModel.DataAnnotations;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System;
using Unity.GrantManager.GrantApplications;
using System.Collections.Immutable;
using Unity.GrantManager.Locality;

namespace Unity.GrantManager.Web.Views.Shared.Components.ApplicantInfo
{
    public class ApplicantInfoViewModel : PageModel
    {
        public static ImmutableDictionary<string, string> DropdownList => 
            ImmutableDictionary.CreateRange(new[]
            {
                new KeyValuePair<string, string>("VALUE1", "Value 1"),
                new KeyValuePair<string, string>("VALUE2", "Value 2"),
            });

        public List<SelectListItem> OrganizationTypeList { get; set; } = FormatOptionsList(ProjectInfoOptionsList.OrganizationTypeList);
        public List<SelectListItem> OrgBookStatusList { get; set; } = FormatOptionsList(ProjectInfoOptionsList.OrgBookStatusList);
        public List<SelectListItem> ApplicationSectorsList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> ApplicationSubSectorsList { get; set; } = new List<SelectListItem>();
        
        public Guid ApplicationId { get; set; }
        public Guid ApplicantId { get; set; }
        public Guid ApplicationFormId { get; set; }
        public Guid ApplicationFormVersionId { get; set; }

        public List<SectorDto> ApplicationSectors { get; set; } = new List<SectorDto>();
        public bool IsFinalDecisionMade { get; set; }
        public ApplicantInfoViewModelModel ApplicantInfo { get; set; } = new();


        public class ApplicantInfoViewModelModel
        {

            [Display(Name = "ApplicantInfoView:ApplicantInfo.OrgName")]
            public string? OrgName { get; set; }

            [Display(Name = "ApplicantInfoView:ApplicantInfo.OrgNumber")]
            public string? OrgNumber { get; set; }

            [Display(Name = "ApplicantInfoView:ApplicantInfo.OrgBookStatus")]
            [SelectItems(nameof(OrgBookStatusList))]
            public string? OrgStatus { get; set; }

            [Display(Name = "ApplicantInfoView:ApplicantInfo.OrganizationType")]
            [SelectItems(nameof(OrganizationTypeList))]
            public string? OrganizationType { get; set; }

            [Display(Name = "ApplicantInfoView:ApplicantInfo.OrganizationSize")]
            public string? OrganizationSize { get; set; }

            [Display(Name = "ApplicantInfoView:ApplicantInfo.Sector")]
            [SelectItems(nameof(ApplicationSectorsList))]
            public string? Sector { get; set; }

            [Display(Name = "ApplicantInfoView:ApplicantInfo.SubSector")]
            [SelectItems(nameof(ApplicationSubSectorsList))]
            public string? SubSector { get; set; }

            [Display(Name = "ApplicantInfoView:ApplicantInfo.ContactFullName")]
            [MaxLength(600, ErrorMessage = "Must be a maximum of 6 characters")]
            public string? ContactFullName { get; set; }

            [Display(Name = "ApplicantInfoView:ApplicantInfo.ContactTitle")]
            public string? ContactTitle { get; set; }

            [Display(Name = "ApplicantInfoView:ApplicantInfo.ContactEmail")]
            [DataType(DataType.EmailAddress, ErrorMessage = "Provided email is not valid")]
            public string? ContactEmail { get; set; }

            [Display(Name = "ApplicantInfoView:ApplicantInfo.ContactBusinessPhone")]
            [DataType(DataType.PhoneNumber, ErrorMessage = "Invalid Phone Number")]
            [RegularExpression(@"^(\+\s?)?((?<!\+.*)\(\+?\d+([\s\-\.]?\d+)?\)|\d+)([\s\-\.]?(\(\d+([\s\-\.]?\d+)?\)|\d+))*(\s?(x|ext\.?)\s?\d+)?$", ErrorMessage = "Invalid Phone Number.")]
            public string? ContactBusinessPhone { get; set; }

            [Display(Name = "ApplicantInfoView:ApplicantInfo.ContactCellPhone")]
            [DataType(DataType.PhoneNumber, ErrorMessage = "Invalid Phone Number")]
            [RegularExpression(@"^(\+\s?)?((?<!\+.*)\(\+?\d+([\s\-\.]?\d+)?\)|\d+)([\s\-\.]?(\(\d+([\s\-\.]?\d+)?\)|\d+))*(\s?(x|ext\.?)\s?\d+)?$", ErrorMessage = "Invalid Phone Number.")]
            public string? ContactCellPhone { get; set; }


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

            [Display(Name = "ApplicantInfoView:ApplicantInfo.SectorSubSectorIndustryDesc")]
            [TextArea(Rows = 2)]
            public string? SectorSubSectorIndustryDesc { get; set; }

            [Display(Name = "ApplicantInfoView:ApplicantInfo.Street")]
            public string? PhysicalAddressStreet { get; set; }

            [Display(Name = "ApplicantInfoView:ApplicantInfo.Street2")]
            public string? PhysicalAddressStreet2 { get; set; }

            [Display(Name = "ApplicantInfoView:ApplicantInfo.Unit")]
            public string? PhysicalAddressUnit { get; set; }

            [Display(Name = "ApplicantInfoView:ApplicantInfo.City")]
            public string? PhysicalAddressCity { get; set; }

            [Display(Name = "ApplicantInfoView:ApplicantInfo.Province")]
            public string? PhysicalAddressProvince { get; set; }

            [Display(Name = "ApplicantInfoView:ApplicantInfo.PostalCode")]
            public string? PhysicalAddressPostalCode { get; set; }


            [Display(Name = "ApplicantInfoView:ApplicantInfo.Street")]
            public string? MailingAddressStreet { get; set; }

            [Display(Name = "ApplicantInfoView:ApplicantInfo.Street2")]
            public string? MailingAddressStreet2 { get; set; }

            [Display(Name = "ApplicantInfoView:ApplicantInfo.Unit")]
            public string? MailingAddressUnit { get; set; }

            [Display(Name = "ApplicantInfoView:ApplicantInfo.City")]
            public string? MailingAddressCity { get; set; }

            [Display(Name = "ApplicantInfoView:ApplicantInfo.Province")]
            public string? MailingAddressProvince { get; set; }

            [Display(Name = "ApplicantInfoView:ApplicantInfo.PostalCode")]
            public string? MailingAddressPostalCode { get; set; }
        }

        public static List<SelectListItem> FormatOptionsList(ImmutableDictionary<string, string> optionsList)
        {
            List<SelectListItem> optionsFormattedList = new();
            foreach (KeyValuePair<string, string> entry in optionsList)
            {
                optionsFormattedList.Add(new SelectListItem { Value = entry.Key, Text = entry.Value });
            }
            return optionsFormattedList;
        }
    }
}

