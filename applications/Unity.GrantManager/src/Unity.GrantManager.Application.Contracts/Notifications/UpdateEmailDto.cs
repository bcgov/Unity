using System;
using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.Emails
{
    public class UpdateEmailDto
    {
        public Guid OwnerId { get; set; }

        [StringLength(2000)]
        [Required]
        [MinLength(1)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public Guid EmailId { get; set; }
    }
}
