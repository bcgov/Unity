using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Unity.GrantManager.ApplicantProfile;

namespace Unity.GrantManager.ApplicationForms;

public class ExternalLinksConfigDto : IValidatableObject
{
    public const int MaxRelatedLinks = 8;

    public ExternalLinkConfigDto? RenewalLink { get; set; }

    public List<ExternalLinkConfigDto> RelatedLinks { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (RenewalLink is { Published: true } && !ExternalLinkUriValidator.IsValidHttpUri(RenewalLink.Uri))
        {
            yield return new ValidationResult(
                "Renewal link visibility cannot be enabled without a valid renewal link URL.",
                [nameof(RenewalLink)]);
        }

        if (RelatedLinks.Count > MaxRelatedLinks)
        {
            yield return new ValidationResult(
                $"A maximum of {MaxRelatedLinks} related links is allowed.",
                [nameof(RelatedLinks)]);
        }

        if (RenewalLink is not null && RenewalLink.ExternalLinkType != ExternalLinkType.Renewal)
        {
            yield return new ValidationResult(
                "Renewal link must be of type Renewal.",
                [nameof(RenewalLink)]);
        }

        if (RelatedLinks.Exists(l => l.ExternalLinkType != ExternalLinkType.Related))
        {
            yield return new ValidationResult(
                "Related links must all be of type Related.",
                [nameof(RelatedLinks)]);
        }

        for (var i = 0; i < RelatedLinks.Count; i++)
        {
            var link = RelatedLinks[i];
            if (link.Published && !ExternalLinkUriValidator.IsValidHttpUri(link.Uri))
            {
                yield return new ValidationResult(
                    $"Related link visibility cannot be enabled without a valid URL (item {i + 1}).",
                    [nameof(RelatedLinks)]);
            }
        }
    }
}
