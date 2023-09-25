using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form;

namespace Unity.GrantManager.Web.ViewModels
{
    public class CreateUpdateApplicationFormViewModel
    {
        [SelectItems(nameof(IntakesList))]
        public Guid IntakeId { get; set; }

        [Required]
        public List<SelectListItem> IntakesList { get; set; } = new List<SelectListItem>();       
        
        [Required]
        [DisplayName("Common:Name")]
        public string ApplicationFormName { get; set; } = string.Empty;

        [DisplayName("Common:Description")]
        public string? ApplicationFormDescription { get; set; }

        [Required]
        [DisplayName("ApplicationForms:ChefsFormId")]
        public string? ChefsApplicationFormGuid { get; set; }

        [DisplayName("ApplicationForms:ChefsCriteriaFormId")]
        public string? ChefsCriteriaFormGuid { get; set; } = Guid.Empty.ToString();

        [Required]
        [DisplayName("ApplicationForms:ChefsFormApiKey")]
        public string? ApiKey { get; set; }
    }
}
