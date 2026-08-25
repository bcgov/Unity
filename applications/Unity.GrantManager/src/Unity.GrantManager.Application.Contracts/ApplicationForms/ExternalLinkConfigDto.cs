using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Unity.GrantManager.ApplicantProfile;

namespace Unity.GrantManager.ApplicationForms;

public class ExternalLinkConfigDto : IValidatableObject
{
    [Required]
    [MaxLength(2048)]
    public string Uri { get; set; } = string.Empty;

    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(512)]
    public string Description { get; set; } = string.Empty;

    public bool Published { get; set; }

    public ExternalLinkType ExternalLinkType { get; set; } = ExternalLinkType.Related;

    public int Order { get; set; } = -1;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!ExternalLinkUriValidator.IsValidHttpUri(Uri))
        {
            yield return new ValidationResult(
                "Uri must be an absolute, well-formed http or https URL.",
                [nameof(Uri)]);
        }
    }
}
