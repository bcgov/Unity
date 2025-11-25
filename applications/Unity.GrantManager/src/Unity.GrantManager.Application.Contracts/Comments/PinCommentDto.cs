using System;
using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.Comments;

public class PinCommentDto
{
    [Required]
    public Guid OwnerId { get; set; }
    
    [Required]
    public CommentType CommentType { get; set; }
}
