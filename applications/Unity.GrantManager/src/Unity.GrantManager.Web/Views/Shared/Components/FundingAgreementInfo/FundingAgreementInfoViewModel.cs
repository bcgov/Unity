using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.Web.Views.Shared.Components.FundingAgreementInfo
{
    public class FundingAgreementInfoViewModel : PageModel
    {
        public Guid ApplicationId { get; set; }
        public Guid ApplicationFormId { get; set; }
        public Guid ApplicationFormVersionId { get; set; }

        public FundingAgreementInfoViewModelModel FundingAgreementInfo { get; set; } = new();

        public class FundingAgreementInfoViewModelModel
        {

            [Display(Name = "FundingAgreementInfoView:FundingAgreementInfo.ContractNumber")]
            [RegularExpression(@"^[a-zA-Z0-9]*$", ErrorMessage = "Invalid Contract Number.")]
            public string? ContractNumber { get; set; }

            [Display(Name = "FundingAgreementInfoView:FundingAgreementInfo.ContractExecutionDate")]
            public DateTime? ContractExecutionDate { get; set; }

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

