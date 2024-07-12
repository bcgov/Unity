using System.ComponentModel.DataAnnotations;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System;
using Unity.GrantManager.GrantApplications;
using System.Collections.Immutable;
using Unity.GrantManager.Locality;

namespace Unity.GrantManager.Web.Views.Shared.Components.ProjectInfo
{
    public class ProjectInfoViewModel : PageModel
    {
        public static ImmutableDictionary<string, string> DropdownList => 
            ImmutableDictionary.CreateRange(new[]
            {
                new KeyValuePair<string, string>("VALUE1", "Value 1"),
                new KeyValuePair<string, string>("VALUE2", "Value 2"),
            });

        public List<SelectListItem> ForestryList { get; set; } = FormatOptionsList(ProjectInfoOptionsList.ForestryList);

        public List<SelectListItem> ForestryFocusList { get; set; } = FormatOptionsList(ProjectInfoOptionsList.ForestryFocusList);

        public List<SelectListItem> AcquisitionList { get; set; } = FormatOptionsList(ProjectInfoOptionsList.AcquisitionList);

        public List<SelectListItem> ApplicationSectorsList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> ApplicationSubSectorsList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> EconomicRegionList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> ElectoralDistrictList { get; set; } = new List<SelectListItem>();
        
        public Guid ApplicationId { get; set; }
        public Guid ApplicationFormId { get; set; }
        public Guid ApplicationFormVersionId { get; set; }
        public List<SectorDto> ApplicationSectors { get; set; } = new List<SectorDto>();
        public bool IsFinalDecisionMade { get; set; }
        public ProjectInfoViewModelModel ProjectInfo { get; set; } = new();

        public List<EconomicRegionDto> EconomicRegions  { get; set; } = new List<EconomicRegionDto>();
        public List<RegionalDistrictDto> RegionalDistricts  { get; set; } = new List<RegionalDistrictDto>();
        public List<SelectListItem> CommunityList { get; set; } = new List<SelectListItem>();
        public List<CommunityDto> Communities { get; set; } = new List<CommunityDto>();
        public List<SelectListItem> RegionalDistrictList { get; set; } = new List<SelectListItem>();
        public bool IsEditGranted { get; set; }
        public bool IsPostEditFieldsAllowed { get; set; }

        public class ProjectInfoViewModelModel
        {

            [Display(Name = "ProjectInfoView:ProjectInfo.ProjectName")]
            [MaxLength(255, ErrorMessage = "Must be a maximum of 255 characters")]
            public string? ProjectName { get; set; }

            [Display(Name = "ProjectInfoView:ProjectInfo.ProjectSummary")]
            [TextArea(Rows = 1)]
            public string? ProjectSummary { get; set; }

            [Display(Name = "ProjectInfoView:ProjectInfo.ProjectStartDate")]
            public DateTime? ProjectStartDate { get; set; }

            [Display(Name = "ProjectInfoView:ProjectInfo.ProjectEndDate")]
            public DateTime? ProjectEndDate { get; set; }

            [Display(Name = "ProjectInfoView:ProjectInfo.RequestedAmount")]
            [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
            public decimal? RequestedAmount { get; set; }

            [Display(Name = "ProjectInfoView:ProjectInfo.TotalProjectBudget")]
            [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
            public decimal? TotalProjectBudget { get; set; }

            [Display(Name = "ProjectInfoView:ProjectInfo.PercentageTotalProjectBudget")]    
            public double? PercentageTotalProjectBudget { get; set; }

            [Display(Name = "ProjectInfoView:ProjectInfo.ProjectFundingTotal")]
            [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
            public decimal? ProjectFundingTotal { get; set; }

            [Display(Name = "ProjectInfoView:ProjectInfo.Sector")]
            [SelectItems(nameof(ApplicationSectorsList))]
            public string? Sector { get; set; }

            [Display(Name = "ProjectInfoView:ProjectInfo.SubSector")]
            [SelectItems(nameof(ApplicationSubSectorsList))]
            public string? SubSector { get; set; }

            [Display(Name = "ProjectInfoView:ProjectInfo.EconomicRegion")]
            [SelectItems(nameof(EconomicRegionList))]
            public string? EconomicRegion { get; set; }

            [Display(Name = "ProjectInfoView:ProjectInfo.ElectoralDistrict")]
            [SelectItems(nameof(ElectoralDistrictList))]
            public string? ElectoralDistrict { get; set; }

            [Display(Name = "ProjectInfoView:ProjectInfo.CommunityPopulation")]
            public int? CommunityPopulation { get; set; } = 0;

            [Display(Name = "ProjectInfoView:ProjectInfo.Acquisition")]
            [SelectItems(nameof(AcquisitionList))]
            public string? Acquisition { get; set; }

            [Display(Name = "ProjectInfoView:ProjectInfo.Forestry")]
            [SelectItems(nameof(ForestryList))]
            public string? Forestry { get; set; }

            [Display(Name = "ProjectInfoView:ProjectInfo.ForestryFocus")]
            [SelectItems(nameof(ForestryFocusList))]
            public string? ForestryFocus { get; set; }

            [Display(Name = "ProjectInfoView:ProjectInfo.RegionalDistrict")]
            [SelectItems(nameof(RegionalDistrictList))]
            public string? RegionalDistrict { get; set; }

            [Display(Name = "ProjectInfoView:ProjectInfo.Community")]
            [SelectItems(nameof(CommunityList))]
            public string? Community { get; set; }

            [Display(Name = "ProjectInfoView:ProjectInfo.ContactFullName")]
            [MaxLength(600, ErrorMessage = "Must be a maximum of 6 characters")]
            public string? ContactFullName { get; set; }

            [Display(Name = "ProjectInfoView:ProjectInfo.ContactTitle")]
            public string? ContactTitle { get; set; }

            [Display(Name = "ProjectInfoView:ProjectInfo.ContactEmail")]
            [DataType(DataType.EmailAddress, ErrorMessage = "Provided email is not valid")]
            public string? ContactEmail { get; set; }

            [Display(Name = "ProjectInfoView:ProjectInfo.ContactBusinessPhone")]
            [DataType(DataType.PhoneNumber, ErrorMessage = "Invalid Phone Number")]
            [RegularExpression(@"^(\+\s?)?((?<!\+.*)\(\+?\d+([\s\-\.]?\d+)?\)|\d+)([\s\-\.]?(\(\d+([\s\-\.]?\d+)?\)|\d+))*(\s?(x|ext\.?)\s?\d+)?$", ErrorMessage = "Invalid Phone Number.")]
            public string? ContactBusinessPhone { get; set; }

            [Display(Name = "ProjectInfoView:ProjectInfo.ContactCellPhone")]
            [DataType(DataType.PhoneNumber, ErrorMessage = "Invalid Phone Number")]
            [RegularExpression(@"^(\+\s?)?((?<!\+.*)\(\+?\d+([\s\-\.]?\d+)?\)|\d+)([\s\-\.]?(\(\d+([\s\-\.]?\d+)?\)|\d+))*(\s?(x|ext\.?)\s?\d+)?$", ErrorMessage = "Invalid Phone Number.")]
            public string? ContactCellPhone { get; set; }



            [Display(Name = "ProjectInfoView:ProjectInfo.ContractNumber")]
            [RegularExpression(@"^[a-zA-Z0-9]*$", ErrorMessage = "Invalid Contract Number.")]
            public string? ContractNumber { get; set; }

            [Display(Name = "ProjectInfoView:ProjectInfo.ContractExecutionDate")]
            public DateTime? ContractExecutionDate { get; set; }
            [Display(Name = "ProjectInfoView:ProjectInfo.Place")]
            [StringLength(50)]
            public string? Place { get; set; }
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

