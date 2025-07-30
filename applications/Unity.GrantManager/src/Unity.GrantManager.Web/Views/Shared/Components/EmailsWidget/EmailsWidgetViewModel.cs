using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.AspNetCore.Mvc.UI.Bootstrap.TagHelpers.Form;

namespace Unity.GrantManager.Web.Views.Shared.Components.EmailsWidget
{
    public class EmailsWidgetViewModel
    {
        [Required]
        [Placeholder("To")]
        [DisplayName("Email To")]
        public string EmailTo { get; set; } = string.Empty;

        [Required]
        [Placeholder("From")]
        [DataType(DataType.EmailAddress)]
        [DisplayName("Email From")]
        public string EmailFrom { get; set; } = string.Empty;

        [Placeholder("CC")]
        [DisplayName("Email CC")]
        public string? EmailCC { get; set; }

        [Placeholder("BCC")]
        [DisplayName("Email BCC")]
        public string? EmailBCC { get; set; }
        
        [Required]
        [Placeholder("Subject")]
        [DisplayName("Email Subject")]
        [MaxLength(1023)]
        public string EmailSubject { get; set; } = string.Empty;


        [TextArea(Rows = 4)]
        [Placeholder("Body of email")]
        [DisplayName("Email Body")]
        [Required]
        public string EmailBody { get; set; } = string.Empty;
        public Guid OwnerId { get; set; }
        public Guid ApplicationId { get; set; }
        public Guid EmailId { get; set; } = Guid.Empty;
        public Guid CurrentUserId { get; set; }

        [DisplayName("Email Template")]
        public Guid? EmailTemplate { get; set; }

        [DisplayName("Email Template")]
        public string? EmailTemplateName { get; set; }

        public List<SelectListItem> TemplatesList { get; set; } = new();
    }
}
