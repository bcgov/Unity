using System.ComponentModel.DataAnnotations;

namespace Unity.GrantManager.Comments
{
    public class CreateCommentDto
    {
        [StringLength(2000)]
        [Required]
        [MinLength(1)]
        public string Comment { get; set; } = string.Empty;        
    }
}
