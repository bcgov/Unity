using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Unity.AI.Prompts;

public class CreateUpdateAIPromptDto
{
    [Required]
    [MaxLength(200)]
    [DisplayName("PromptName")]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    [DisplayName("PromptDescription")]
    public string? Description { get; set; }

    [DisplayName("PromptType")]
    public PromptType Type { get; set; }

    [DisplayName("PromptIsActive")]
    public bool IsActive { get; set; } = true;
}
