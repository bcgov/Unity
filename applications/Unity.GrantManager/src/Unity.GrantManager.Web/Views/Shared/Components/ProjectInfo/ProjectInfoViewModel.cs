using System.ComponentModel.DataAnnotations;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System;
using Unity.GrantManager.GrantApplications;
using System.Collections.Immutable;

namespace Unity.GrantManager.Web.Views.Shared.Components.ProjectInfo
{
    public class ProjectInfoViewModel : PageModel
    {
        public List<SelectListItem> FundingRiskList { get; set; } = FormatOptionsList(AssessmentResultsOptionsList.FundingList);
        public List<SelectListItem> DueDilligenceList { get; set; } = FormatOptionsList(AssessmentResultsOptionsList.DueDilligenceList);
        public List<SelectListItem> AssessmentResultStatusList { get; set; } = FormatOptionsList(AssessmentResultsOptionsList.AssessmentResultStatusList);
        public List<SelectListItem> DeclineRationalActionList { get; set; } = FormatOptionsList(AssessmentResultsOptionsList.DeclineRationalActionList);
        public List<SelectListItem> RecommendationActionList { get; set; } = FormatOptionsList(AssessmentResultsOptionsList.RecommendationActionList);

        public Guid ApplicationId { get; set; }
        public bool IsFinalDecisionMade { get; set; }
        public ProjectInfoViewModelModel ProjectInfo { get; set; } = new();

        public class ProjectInfoViewModelModel
        {

            [Display(Name = "ProjectInfoView:ProjectInfo.ProjectName")]
            public string? ProjectName { get; set; }

            [Display(Name = "ProjectInfoView:ProjectInfo.ProjectSummary")]
            [TextArea(Rows = 1)]
            public string? ProjectSummary { get; set; }

            [Display(Name = "ProjectInfoView:ProjectInfo.ProjectStartDate")]
            public string? ProjectStartDate { get; set; }

            [Display(Name = "ProjectInfoView:ProjectInfo.ProjectEndDate")]
            public string? ProjectEndDate { get; set; }

            [Display(Name = "ProjectInfoView:ProjectInfo.ProjectLocation")]
            public string? ProjectLocation { get; set; }

            [Display(Name = "ProjectInfoView:ProjectInfo.RequestedAmount")]
            [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
            public decimal? RequestedAmount { get; set; }

            [Display(Name = "ProjectInfoView:ProjectInfo.TotalProjectBudget")]
            [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
            public decimal? TotalProjectBudget { get; set; }

            [Display(Name = "ProjectInfoView:ProjectInfo.PercentageTotalProjectBudget")]    
            public decimal? PercentageTotalProjectBudget { get; set; }

            [Display(Name = "ProjectInfoView:ProjectInfo.ProjectFundingTotal")]
            [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
            public decimal? ProjectFundingTotal { get; set; }

            [Display(Name = "ProjectInfoView:ProjectInfo.Sector")]
            [SelectItems(nameof(FundingRiskList))]
            public string? Sector { get; set; }

            [Display(Name = "ProjectInfoView:ProjectInfo.SubSector")]
            [SelectItems(nameof(DueDilligenceList))]
            public string? SubSector { get; set; }

            [Display(Name = "ProjectInfoView:ProjectInfo.Community")]
            public string? Community { get; set; }

            [Display(Name = "ProjectInfoView:ProjectInfo.EconomicRegion")]
            [SelectItems(nameof(RecommendationActionList))]
            public string? EconomicRegion { get; set; }

            [Display(Name = "ProjectInfoView:ProjectInfo.ElectoralDistrict")]
            [SelectItems(nameof(DeclineRationalActionList))]
            public string? ElectoralDistrict { get; set; }

            [Display(Name = "ProjectInfoView:ProjectInfo.CommunityPopulation")]
            public string? CommunityPopulation { get; set; }

            [Display(Name = "ProjectInfoView:ProjectInfo.Acquisition")]
            public string? Acquisition { get; set; }

            [Display(Name = "ProjectInfoView:ProjectInfo.Forestry")]
            [SelectItems(nameof(DeclineRationalActionList))]
            public string? Forestry { get; set; }

            [Display(Name = "ProjectInfoView:ProjectInfo.ForestryFocus")]
            [SelectItems(nameof(DeclineRationalActionList))]
            public string? ForestryFocus { get; set; }

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

