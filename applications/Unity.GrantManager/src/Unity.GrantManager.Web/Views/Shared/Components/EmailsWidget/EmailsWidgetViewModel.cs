using System;
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
        
        [Required]
        [Placeholder("Subject")]
        [DisplayName("Email Subject")]
        [MaxLength(1023)]
        public string EmailSubject { get; set; } = string.Empty;


        [TextArea(Rows = 4)]
        [Placeholder("Body of email")]
        [DisplayName("Email Body")]
        [MaxLength(40000)]
        [Required]
        public string EmailBody { get; set; } = string.Empty;
        public Guid OwnerId { get; set; }
        public Guid ApplicationId { get; set; }
        public Guid EmailId { get; set; } = Guid.Empty;
        public Guid CurrentUserId { get; set; }
    }
}
