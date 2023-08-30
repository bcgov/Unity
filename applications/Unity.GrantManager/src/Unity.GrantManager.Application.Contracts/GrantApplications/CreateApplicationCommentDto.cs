using System;
using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.GrantApplications
{
    public class CreateApplicationCommentDto
    {
        [StringLength(2000)]
        [Required]
        [MinLength(1)]
        public string Comment { get; set; } = string.Empty;

        [Required]
        public Guid ApplicationId { get; set; }
    }
}
