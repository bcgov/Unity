using System;
using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.Comments
{
    public class UpdateCommentDto
    {
        [StringLength(2000)]
        [Required]
        [MinLength(1)]
        public string Comment { get; set; } = string.Empty;

        [Required]
        public Guid CommentId { get; set; }
    }
}
