using System.ComponentModel.DataAnnotations;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System;
using Unity.GrantManager.GrantApplications;

namespace Unity.GrantManager.Web.Views.Shared.Components.AssessmentResults
{
    public class AssessmentResultsPageModel : PageModel
    {
        public List<SelectListItem> FundingRiskList { get; set; } = FormatOptionsList(AssessmentResultsOptionsList.FundingList);
        public List<SelectListItem> DueDiligenceList { get; set; } = FormatOptionsList(AssessmentResultsOptionsList.DueDiligenceList);
        public List<SelectListItem> AssessmentResultStatusList { get; set; } = FormatOptionsList(AssessmentResultsOptionsList.AssessmentResultStatusList);
        public List<SelectListItem> DeclineRationalActionList { get; set; } = FormatOptionsList(AssessmentResultsOptionsList.DeclineRationalActionList);
        public List<SelectListItem> SubStatusActionList { get; set; } = FormatOptionsList(AssessmentResultsOptionsList.SubStatusActionList);

        public List<SelectListItem> RiskRankingList { get; set; } = FormatOptionsList(AssessmentResultsOptionsList.RiskRankingList);
        public Guid ApplicationId { get; set; }
        public Guid ApplicationFormId { get; set; }
        public Guid ApplicationFormVersionId { get; set; }

        public AssessmentResultsModel AssessmentResults { get; set; } = new();
        public bool IsEditGranted { get; set; }
        public bool IsPostEditFieldsAllowed { get; set; }

        public class AssessmentResultsModel
        {

            [Display(Name = "AssessmentResultsView:AssessmentResultsForm.ProjectSummary")]
            [TextArea(Rows = 1)]
            public string? ProjectSummary { get; set; }

            [Display(Name = "AssessmentResultsView:AssessmentResultsForm.TotalScore")]
            public int? TotalScore { get; set; }

            
            [Display(Name = "AssessmentResultsView:AssessmentResultsForm.RequestedAmount")]
            [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
            public decimal? RequestedAmount { get; set; }

            [Display(Name = "AssessmentResultsView:AssessmentResultsForm.TotalProjectBudget")]
            [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
            public decimal? TotalProjectBudget { get; set; }

            [Display(Name = "AssessmentResultsView:AssessmentResultsForm.RecommendedAmount")]
            [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
            public decimal? RecommendedAmount { get; set; }

            [Display(Name = "AssessmentResultsView:AssessmentResultsForm.ApprovedAmount")]
            [DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
            public decimal? ApprovedAmount { get; set; }

            [Display(Name = "AssessmentResultsView:AssessmentResultsForm.LikelihoodOfFunding")]
            [SelectItems(nameof(FundingRiskList))]
            public string? LikelihoodOfFunding { get; set; }

            [Display(Name = "AssessmentResultsView:AssessmentResultsForm.DueDiligenceStatus")]
            [SelectItems(nameof(DueDiligenceList))]
            public string? DueDiligenceStatus { get; set; }

            [Display(Name = "AssessmentResultsView:AssessmentResultsForm.SubStatus")]
            [SelectItems(nameof(SubStatusActionList))]
            public string? SubStatus { get; set; }

            [Display(Name = "AssessmentResultsView:AssessmentResultsForm.DeclineRational")]
            [SelectItems(nameof(DeclineRationalActionList))]
            public string? DeclineRational { get; set; }

            [Display(Name = "AssessmentResultsView:AssessmentResultsForm.Notes")]
            [TextArea(Rows = 2)]
            [StringLength(2000)]
            public string? Notes { get; set; }

            [Display(Name = "AssessmentResultsView:AssessmentResultsForm.AssessmentResult")]
            [SelectItems(nameof(AssessmentResultStatusList))]
            public string? AssessmentResultStatus { get; set; }
            [Display(Name = "AssessmentResultsView:AssessmentResultsForm.FinalDecisionDate")]
            public DateTime? FinalDecisionDate { get; set; }
            [Display(Name = "AssessmentResultsView:AssessmentResultsForm.DueDate")]
            public DateTime? DueDate { get; set; }
            [Display(Name = "AssessmentResultsView:AssessmentResultsForm.NotificationDate")]
            public DateTime? NotificationDate { get; set; }

            [Display(Name = "AssessmentResultsView:AssessmentResultsForm.RiskRanking")]
            [SelectItems(nameof(RiskRankingList))]
            public string? RiskRanking { get; set; }
        }

        public static List<SelectListItem> FormatOptionsList(Dictionary<string, string> optionsList)
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

