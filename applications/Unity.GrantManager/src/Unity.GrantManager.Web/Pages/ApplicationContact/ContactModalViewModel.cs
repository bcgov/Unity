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
        public string ContactFullName { get; set; } = string.Empty;

        [DisplayName("ApplicationContact:Title")]
        public string? ContactTitle { get; set; }

        [DisplayName("ApplicationContact:Email")]
        public string? ContactEmail { get; set; }

        [DisplayName("ApplicationContact:MobilePhone")]
        public string? ContactMobilePhone { get; set; }

        [DisplayName("ApplicationContact:WorkPhone")]
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
