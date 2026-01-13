using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Unity.Flex.WorksheetLinks;
using Unity.Flex.Worksheets;

namespace Unity.GrantManager.Web.Views.Shared.Components.CustomFields
{

    public class CustomFieldsViewModel
    {
        public List<SelectListItem> ScoresheetOptionsList { get; set; } = [];

        [Required]
        public Guid ChefsFormVersionId { get; set; }

        public string? FormName { get; set; }
        public string? Version { get; set; }

        public string? AssessmentInfoSlotIds { get; set; }
        public string? ProjectInfoSlotIds { get; set; }
        public string? ApplicantInfoSlotIds { get; set; }
        public string? PaymentInfoSlotIds { get; set; }
        public string? FundingAgreementInfoSlotIds { get; set; }
        public string? CustomTabsSlotIds { get; set; }

        public List<WorksheetLinkDto>? WorksheetLinks { get; set; }
        public List<WorksheetBasicDto>? PublishedWorksheets { get; set; }

        public List<WorksheetLinkDto>? AssessmentInfoLinks { get; set; }
        public List<WorksheetLinkDto>? ApplicantInfoLinks { get; set; }
        public List<WorksheetLinkDto>? ProjectInfoLinks { get; set; }
        public List<WorksheetLinkDto>? PaymentInfoLinks { get; set; }
        public List<WorksheetLinkDto>? FundingAgreementInfoLinks { get; set; }
        public List<WorksheetLinkDto>? CustomTabLinks { get; set; }

        [Display(Name = "")]
        public Guid? ScoresheetId { get; set; }
    }
}