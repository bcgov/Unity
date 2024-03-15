using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Unity.GrantManager.GrantApplications;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form;

namespace Unity.GrantManager.Web.Pages.ApplicationContact
{
    public class ContactModalViewModel
    {
        [DisplayName("ApplicationContact:Type")]
        [SelectItems(nameof(ContactTypeList))]
        [Required]
        public string ContactType { get; set; } = string.Empty;

        public List<SelectListItem> ContactTypeList { get; set; } = FormatOptionsList(ApplicationContactOptionList.ContactTypeList);

        [DisplayName("ApplicationContact:FullName")]
        [StringLength(100)]
        [Required]
        public string ContactFullName { get; set; } = string.Empty;

        [DisplayName("ApplicationContact:Title")]
        [StringLength(50)]
        public string? ContactTitle { get; set; }

        [DisplayName("ApplicationContact:Email")]
        [EmailAddress]
        public string? ContactEmail { get; set; }

        [DisplayName("ApplicationContact:MobilePhone")]
        [StringLength(10)]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Enter a valid phone number")]
        public string? ContactMobilePhone { get; set; }

        [DisplayName("ApplicationContact:WorkPhone")]
        [StringLength(10)]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Enter a valid phone number")]
        [Phone]
        public string? ContactWorkPhone { get; set; }

        [HiddenInput]
        public Guid? ApplicationId { get; set; }
        
        [HiddenInput]
        public Guid? Id { get; set; }


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
