using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form;

namespace Unity.GrantManager.Web.Pages.ApplicationForms.ViewModels
{
    public class CreateUpdateApplicationFormViewModel
    {
        [SelectItems(nameof(IntakesList))]
        public Guid IntakeId { get; set; }

        [Required]
        public List<SelectListItem> IntakesList { get; set; } = new List<SelectListItem>();

        [DisabledInput]
        [DisplayName("Common:Name")]
        public string ApplicationFormName { get; set; } = string.Empty;

        [Required]
        [DisplayName("ApplicationForms:ChefsFormId")]
        public string? ChefsApplicationFormGuid { get; set; }

        [Required]
        [DisplayName("ApplicationForms:ChefsFormApiKey")]
        public string? ApiKey { get; set; }

        [DisplayName("Common:Description")]
        public string? ApplicationFormDescription { get; set; }

        [DisplayName("ApplicationForms:Category")]
        public string? Category { get; set; }

        [DisplayName("ApplicationForms:Payable")]
        public bool Payable { get; set; }
    }
}
