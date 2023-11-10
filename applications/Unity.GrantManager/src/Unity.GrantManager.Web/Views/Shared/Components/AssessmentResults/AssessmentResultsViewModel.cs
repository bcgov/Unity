using System.ComponentModel.DataAnnotations;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System;
using Unity.GrantManager.GrantApplications;
using System.Collections.Immutable;

namespace Unity.GrantManager.Web.Views.Shared.Components.AssessmentResults
{
    public class AssessmentResultsPageModel : PageModel
    {
        public List<SelectListItem> FundingRiskList { get; set; } = FormatOptionsList(AssessmentResultsOptionsList.FundingList);
        public List<SelectListItem> DueDilligenceList { get; set; } = FormatOptionsList(AssessmentResultsOptionsList.DueDilligenceList);
        public List<SelectListItem> AssessmentResultStatusList { get; set; } = FormatOptionsList(AssessmentResultsOptionsList.AssessmentResultStatusList);
        public List<SelectListItem> DeclineRationalActionList { get; set; } = FormatOptionsList(AssessmentResultsOptionsList.DeclineRationalActionList);
        public List<SelectListItem> RecommendationActionList { get; set; } = FormatOptionsList(AssessmentResultsOptionsList.RecommendationActionList);

        public Guid ApplicationId { get; set; }
        public bool IsFinalDecisionMade { get; set; }
        public AssessmentResultsModel AssessmentResults { get; set; } = new();
        public bool IsEditGranted { get; set; }
        public bool IsEditApprovedAmount { get; set; }

        public class AssessmentResultsModel
        {

            [TextArea(Rows = 1)]
            public string? ProjectSummary { get; set; }

            public int? TotalScore { get; set; }

            
            [Display(Name = "AssessmentResultsView:AssessmentResultsForm.RequestedAmount")]
            [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
            public decimal? RequestedAmount { get; set; }


            //[DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = true)]
            public decimal? TotalProjectBudget { get; set; }

            [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = true)]
            public decimal? RecommendedAmount { get; set; }

            [DisplayFormat(DataFormatString = "{0:C}", ApplyFormatInEditMode = true)]
            public decimal? ApprovedAmount { get; set; }

            [SelectItems(nameof(FundingRiskList))]
            public string? LikelihoodOfFunding { get; set; }

            [SelectItems(nameof(DueDilligenceList))]
            public string? DueDilligenceStatus { get; set; }

            [SelectItems(nameof(RecommendationActionList))]
            public string? Recommendation { get; set; }

            [SelectItems(nameof(DeclineRationalActionList))]
            public string? DeclineRational { get; set; }

            [TextArea(Rows = 2)]
            [StringLength(200)]
            public string? Notes { get; set; }

            [SelectItems(nameof(AssessmentResultStatusList))]
            public string? AssessmentResultStatus { get; set; }

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

