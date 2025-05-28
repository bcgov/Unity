using System;
using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.Emails
{
    public class CreateEmailDto
    {
        [Required]
        public string EmailTo { get; set; } = string.Empty;

        [Required]
        public string EmailFrom { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(1023)] // Max for CHES
        public string EmailSubject { get; set; } = string.Empty;

        [MaxLength(40000)]
        [Required]
        public string EmailBody { get; set; } = string.Empty;
        public Guid ApplicationId { get; set; }
        public Guid OwnerId { get; set; }
        public Guid EmailId { get; set; } = Guid.Empty;
        public Guid CurrentUserId { get; set; }
    }
}
