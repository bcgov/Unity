using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace Unity.GrantManager.Web.Pages.Template
{

    public class TemplateModalModel : AbpPageModel
    {
        [DisplayName("")]
        public string fileUpload { get; set; } = string.Empty;
        public Guid? CategoryId { get; set; }
        public List<SelectListItem> categoryList { get; set; } = new List<SelectListItem>
        {
            new SelectListItem { Value = "1", Text = "Application" },
            new SelectListItem { Value = "2", Text = "MJF" },
            new SelectListItem { Value = "3", Text = "Report" }
        };
        [DisplayName("Name")]
        public string name { get; set; } = string.Empty;

        [DisplayName("Description")]
        public string description { get; set; } = string.Empty;

        [DisplayName("Preview Link")]
        public string previewLink { get; set; } = string.Empty;

    }
}
